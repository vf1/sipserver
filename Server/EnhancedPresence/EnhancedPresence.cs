using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.IO;

namespace EnhancedPresence
{
	public sealed class EnhancedPresence1 :
		IDisposable
	{
		public delegate void NotifyEventHandler(uint expires, string subscriptionState, OutContent content, Object param);
		public delegate bool IsHomeDomainEventHandler(string uri);

		private Object sync = new Object();
		private Dictionary<string, Publication> publications = new Dictionary<string, Publication>();
		private Dictionary<string, Subscription> subscriptions = new Dictionary<string, Subscription>();
		private ExpiresList expireList = new ExpiresList();
		private Schemas schemas = new Schemas();
		private Timer timer;
		private NotifyEventHandler notifyEvent;

		public EnhancedPresence1(NotifyEventHandler notifyEvent)
		{
			this.notifyEvent = notifyEvent;

			Subscription.ResolvePublicationEvent += ResolvePublication;
			SubscriptionResource.NotifySubscriberEvent += SubscriptionResource_NotifySubscriber;

			this.timer = new Timer(new TimerCallback(ValidateExpired), null, 1000, 1000);
		}

		public void Dispose()
		{
			this.timer.Dispose();
		}

		public Categories ParsePublication(ArraySegment<byte> body)
		{
			lock (sync)
			{
				try
				{
					using (XmlReader reader = CreateXmlReader(body, EnhancedPresenceSchema.Categories))
					{
						return Categories.Parse(reader);
					}
				}
				catch (XmlException e)
				{
					throw new EnhancedPresenceException("Publication - An error occurred while parsing the XML", e);
				}
				catch (XmlSchemaValidationException e)
				{
					throw new EnhancedPresenceException("Publication - Invalid XML document", e);
				}
				catch (Exception e)
				{
					throw new EnhancedPresenceException("Publication - Parse failed", e);
				}
			}
		}

		/// <summary>
		/// ProcessPublication
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="categories"></param>
		/// <param name="endpointId">+sip.instance or Contact header</param>
		/// <returns></returns>
		public OutContent ProcessPublication(string uri, Categories categories, string endpointId)
		{
			lock (sync)
			{
				try
				{
					Publication publication = this.ResolvePublication(uri);

					return new OutContent(publication.Process(categories, endpointId), this.sync);
				}
				catch (Exception e)
				{
					throw new EnhancedPresenceException("ProcessPublication falied", e);
				}
			}
		}

		public BatchSubscribe ParseSubscribe(ArraySegment<byte> body)
		{
			lock (sync)
			{
				try
				{
					if (body == null)
						return null;

					using (XmlReader reader = CreateXmlReader(body, EnhancedPresenceSchema.BatchSubscribe))
					{
						return BatchSubscribe.Parse(reader);
					}
				}
				catch (XmlException e)
				{
					throw new EnhancedPresenceException("BatchSubscribe - An error occurred while parsing the XML", e);
				}
				catch (XmlSchemaValidationException e)
				{
					throw new EnhancedPresenceException("BatchSubscribe - Invalid XML document", e);
				}
				catch (Exception e)
				{
					throw new EnhancedPresenceException("BatchSubscribe - Parse failed", e);
				}
			}
		}

		public List<OutContent> ProcessSubscription(string uri, string dialogId, BatchSubscribe batchSubs, uint expires, string endpointId, object param)
		{
			Rlmi rlmi = null;
			List<Categories> categories;
			List<OutContent> multipart = null;

			lock (sync)
			{
				try
				{
					Subscription subscription;
					if (subscriptions.TryGetValue(dialogId, out subscription) == false)
					{
						subscription = new Subscription(uri, endpointId, param);
						subscriptions.Add(dialogId, subscription);
					}

					subscription.Process(batchSubs, expireList, expires, out rlmi, out categories);

					if (rlmi != null)
					{
						multipart = new List<OutContent>();

						multipart.Add(new OutContent(rlmi, this.sync));

						if (categories != null)
							categories.ForEach(item => multipart.Add(new OutContent(item, this.sync)));
					}

					return multipart;
				}
				catch (Exception e)
				{
					throw new EnhancedPresenceException("ProcessSubscription failed", e);
				}
				finally
				{
					if (rlmi != null)
						rlmi.Dispose();
				}
			}
		}

		/// <summary>
		/// SetContactCard
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="contactCard">new ContactCardCategory() { DisplayName = displayName, Email = email }</param>
		public void SetContactCard(string uri, ContactCardCategory contactCard)
		{
			lock (sync)
			{
				Publication publication = ResolvePublication(uri);

				publication.SetContactCard(contactCard);
			}
		}

		/// <summary>
		/// SetUserProperties
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="userProperties">
		/// new UserPropertiesCategory()
		/// {
		///		Line1 = "tel:+11234567890;ext=67890",
		///		LineType1 = UserPropertiesCategory.LineType.Uc,
		///		FaxNumber = "+12345-FAX-12345",
		///		State = "Rostov-on-Don",
		///		StreetAddress = "Beikerstreet, 10",
		///		WwwHomePage = "http://www.officesip.com",
		///		PostalCode = "347900",
		///		City = "London",
		///		CountryCode = "BR"
		/// }
		/// </param>
		public void SetUserProperties(string uri, UserPropertiesCategory userProperties)
		{
			lock (sync)
			{
				Publication publication = ResolvePublication(uri);

				publication.SetUserProperties(userProperties);
			}
		}

		/// <summary>
		/// ContactRegistered (EP semantic: Endpoint = Contact)
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="endpointId">+sip.instance or Contact header</param>
		/// <param name="epSupported">EP supported</param>
		public void EndpointRegistered(string uri, string endpointId, bool epSupported)
		{
			lock (sync)
			{
				Publication publication = this.ResolvePublication(uri);
				publication.EndpointRegistered(endpointId, epSupported);
			}
		}

		/// <summary>
		/// ContactUnregistered (EP semantic: Endpoint = Contact)
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="endpointId">+sip.instance or contact hearder</param>
		public void EndpointUnregistered(string uri, string endpointId)
		{
			lock (sync)
			{
				Publication publication = null;
				if (this.publications.TryGetValue(uri, out publication))
				{
					publication.EndpointUnregistered(endpointId);
				}

				RemoveSubscriptionByEndpointId(endpointId);
			}
		}

		/// <summary>
		/// AorRegistered (EP semantic: User = Aor)
		/// </summary>
		/// <param name="uri"></param>
		public void UserRegistered(string uri)
		{
			lock (sync)
			{
				Publication publication = this.ResolvePublication(uri);
				publication.UserRegistered();
			}
		}

		/// <summary>
		/// AorUnregistered (EP semantic: User = Aor)
		/// </summary>
		/// <param name="uri"></param>
		public void UserUnregistered(string uri)
		{
			lock (sync)
			{
				Publication publication = null;
				if (this.publications.TryGetValue(uri, out publication))
				{
					publication.UserUnregistered();
				}
			}
		}

		public event Action<string, int> AvailabilityChanged
		{
			add
			{
				Publication.AvailabilityChangedEvent += value;
			}
			remove
			{
				Publication.AvailabilityChangedEvent -= value;
			}
		}

		public int GetAvailability(string uri)
		{
			Publication publication = this.FindPublication(uri);

			if (publication != null)
				return publication.Availability;

			return 0;
		}

		private XmlReader CreateXmlReader(ArraySegment<byte> content, EnhancedPresenceSchema schemaId)
		{
			MemoryStream memoryStream = new MemoryStream(content.Array, content.Offset, content.Count);
			StreamReader streamReader = new StreamReader(memoryStream, System.Text.Encoding.UTF8);

			return XmlReader.Create(streamReader,
				new XmlReaderSettings() { ValidationType = ValidationType.None, });
		}

		private void ValidateExpired(Object state)
		{
			lock (sync)
			{
				SubscriptionResource resource;

				while ((resource = expireList.GetExpired()) != null)
					resource.Destroy();
			}
		}

		private Publication FindPublication(string uri)
		{
			Publication publication = null;

			this.publications.TryGetValue(uri, out publication);

			return publication;
		}

		private Publication ResolvePublication(string uri)
		{
			Publication publication = FindPublication(uri);

			if (publication == null)
			{
				publication = new Publication(uri);
				this.publications.Add(publication.Uri, publication);
			}

			return publication;
		}

		private void SubscriptionResource_NotifySubscriber(SubscriptionResource resource, Categories categories)
		{
			notifyEvent.BeginInvoke(resource.Expires
				, (resource.Expires > 0) ? @"active" : @"terminated"
				, new OutContent(categories, this.sync)
				, resource.Subscription.Param
				, new AsyncCallback(SubscriptionResource_NotifySubscriber_EndInvoke)
				, notifyEvent);
		}

		private void SubscriptionResource_NotifySubscriber_EndInvoke(IAsyncResult ar)
		{
			(ar.AsyncState as NotifyEventHandler).EndInvoke(ar);
			// куда попадет Exception?
		}

		private void RemoveSubscriptionByEndpointId(string endpointId)
		{
			List<string> removeKeys = new List<string>();
			foreach (var subscription in this.subscriptions)
				if (subscription.Value.EndpointId == endpointId)
					removeKeys.Add(subscription.Key);

			foreach (string key in removeKeys)
				subscriptions.Remove(key);
		}
	}
}

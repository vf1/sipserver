using System;
using System.Collections.Generic;
//using System.Linq;

namespace EnhancedPresence
{
	public class Publication :
		ISubscriptionResourceEvents
	{
		private static string[] excludeAggregated = new string[] { @"state" };
		public static event Action<string, int> AvailabilityChangedEvent;

		private List<SubscriptionResource> subscribers;
		private List<Category> published;
		private List<Category> aggregatedStates;

		public Publication(string uri)
		{
			this.Uri = uri;
			this.subscribers = new List<SubscriptionResource>();

			this.published = new List<Category>() {
				new Category() {
					Instance = 0,
					ExpireType = CategoryExpireType.Endpoint,
					State = new StateCategory() { Availability = 0 }
				}
			};

			this.aggregatedStates = new List<Category>() {
				new Category() {
					State = new StateCategory() { Availability = 0 }
				}
			};
			/*
			this.SetUserProperties(
				new UserPropertiesCategory()
				{
					Line1 = "tel:+11234567890;ext=67890",
					LineType1 = UserPropertiesCategory.LineType.Uc,
					FaxNumber = "+12345-FAX-12345",
					State = "Rostov-on-Don",
					StreetAddress = "Beikerstreet, 10",
					WwwHomePage = "http://www.officesip.com",
					PostalCode = "347900",
					City = "London",
					CountryCode = "BR"
				}
			);
			*/
		}

		public void AddResource(SubscriptionResource subscriber)
		{
			subscriber.PreNotifySubscriber(this.published, excludeAggregated);
			subscriber.PreNotifySubscriber(this.aggregatedStates, null);
			subscriber.NotifySubscriber();

			subscribers.Add(subscriber);
		}

		public void RemoveResource(SubscriptionResource subscriber)
		{
			//if (Categories.Items.Count > 0)
			//	PublicationUpdatedEvent(resource);
			// зачем нотифить сабскрайбера когда он отписывается?
			// тем более список ресурсов фактически не изменился
			// он просто от них отписался, чем нотифить?
			subscribers.Remove(subscriber);
		}

		public void UpdateResource(SubscriptionResource subscriber)
		{
			throw new NotSupportedException();
		}

		public void UserRegistered()
		{
		}

		public void UserUnregistered()
		{
			if (this.published.RemoveAll(category => category.ExpireType == CategoryExpireType.Endpoint
				|| category.ExpireType == CategoryExpireType.User) > 0)
			{
				if (this.AgregateState())
					subscribers.ForEach(
						subscriber =>
						{
							subscriber.PreNotifySubscriber(this.aggregatedStates, null);
							subscriber.NotifySubscriber();
						}
					);
			}
		}

		public void EndpointRegistered(string endpointId, bool epSupported)
		{
			if (epSupported == false)
			{
				this.Process(
					new Category()
					{
						Instance = 0,
						ExpireType = CategoryExpireType.Endpoint,
						State = new StateCategory() { Availability = 3500 }
					}
					, endpointId
				);
			}
		}

		public void EndpointUnregistered(string endpointId)
		{
			// kill subscribers here
			//
			if (this.published.RemoveAll(category => category.EndpointId == endpointId) > 0)
			{
				if (this.AgregateState())
					subscribers.ForEach(
						subscriber =>
						{
							subscriber.PreNotifySubscriber(this.aggregatedStates, null);
							subscriber.NotifySubscriber();
						}
					);
			}
		}

		public RoamingData Process(Categories categories, string endpointId)
		{
			this.Process(categories.Items, endpointId);

			return RoamingData.Create(categories);
		}

		public void Process(Category category, string endpointId)
		{
			this.Process(new List<Category> { category }, endpointId);
		}

		private void Process(List<Category> newCategories, string endpointId)
		{
			newCategories.ForEach(
				newCategory =>
				{
					newCategory.Version = 0;
					newCategory.EndpointId = endpointId;
				}
			);

			this.published.RemoveAll(
				oldCategory =>
				{
					Category newCategory = newCategories.Find(
						category => category.IsSame(oldCategory));

					if (newCategory != null)
					{
						newCategory.Version = oldCategory.Version + 1;
						return true;
					}

					return false;
				}
			);

			this.published.AddRange(newCategories);


			bool stateWasUpdated = this.AgregateState();

			this.subscribers.ForEach(
				subscriber =>
				{
					subscriber.PreNotifySubscriber(newCategories, excludeAggregated);
					if (stateWasUpdated)
						subscriber.PreNotifySubscriber(this.aggregatedStates, null);
					subscriber.NotifySubscriber();
				}
			);
		}

		public void SetContactCard(ContactCardCategory contactCard)
		{
			this.Process(
				new Category()
				{
					Instance = 0,
					ExpireType = CategoryExpireType.Static,
					ContactCard = contactCard
				}
				, null
			);
		}

		public void SetUserProperties(UserPropertiesCategory userProperties)
		{
			this.Process(
				new Category()
				{
					Instance = 0,
					ExpireType = CategoryExpireType.Static,
					UserProperties = userProperties
				}
				, null
			);
		}

		private bool AgregateState()
		{
			int maxAvailability = 0;

			this.published.ForEach(
				category =>
				{
					if (category.IsStateCategory())
					{
						if (maxAvailability < category.State.Availability)
							maxAvailability = category.State.Availability;
					}
				}
			);

			bool updated = (this.Availability != maxAvailability);

			this.Availability = maxAvailability;

			return updated;
		}

		public string Uri
		{
			get;
			private set;
		}

		public int Availability
		{
			get
			{
				return this.aggregatedStates[0].State.Availability;
			}
			private set
			{
				if (this.Availability != value)
				{
					this.aggregatedStates[0].State.Availability = value;
					this.aggregatedStates[0].Version += 1;

					if (AvailabilityChangedEvent != null)
						AvailabilityChangedEvent(this.Uri, this.Availability);
				}
			}
		}
	}
}

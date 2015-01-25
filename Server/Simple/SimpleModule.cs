using System;
using System.Threading;
using System.Collections.Generic;
using ThreadSafe = System.Collections.Generic.ThreadSafe;
using Sip.Tools;

namespace Sip.Simple
{
	public sealed class SimpleModule
		: IDisposable
	{
		#region struct PublicationExpiresKey {...}

		struct PublicationExpiresKey
		{
			public PublicationExpiresKey(string id, int sipEtag)
			{
				Id = id;
				SipEtag = sipEtag;
			}

			public readonly string Id;
			public readonly int SipEtag;
		}

		#endregion

		#region class PublicationExpiresKeyEqualityComparer {...}

		class PublicationExpiresKeyEqualityComparer
			: EqualityComparer<PublicationExpiresKey>
		{
			public override bool Equals(PublicationExpiresKey x, PublicationExpiresKey y)
			{
				return x.Id == y.Id && x.SipEtag == y.SipEtag;
			}

			public override int GetHashCode(PublicationExpiresKey obj)
			{
				return obj.Id.GetHashCode() ^ obj.SipEtag.GetHashCode();
			}
		}

		#endregion

		#region struct SubscriptionExpiresKey {...}

		struct SubscriptionExpiresKey
		{
			public SubscriptionExpiresKey(string publicationId, string subscriberId)
			{
				PublicationId = publicationId;
				SubscriptionId = subscriberId;
			}

			public readonly string PublicationId;
			public readonly string SubscriptionId;
		}

		#endregion

		#region class SubscriptionExpiresKeyEqualityComparer {...}

		class SubscriptionExpiresKeyEqualityComparer
			: EqualityComparer<SubscriptionExpiresKey>
		{
			public override bool Equals(SubscriptionExpiresKey x, SubscriptionExpiresKey y)
			{
				return x.PublicationId == y.PublicationId && x.SubscriptionId == y.SubscriptionId;
			}

			public override int GetHashCode(SubscriptionExpiresKey obj)
			{
				return obj.PublicationId.GetHashCode() ^ obj.SubscriptionId.GetHashCode();
			}
		}

		#endregion

		private ThreadSafe.Dictionary<string, Publication> publications;
		private MultiTimer<PublicationExpiresKey> publicationTimer;
		private MultiTimer<SubscriptionExpiresKey> subscriptionTimer;
		private static int sipEtagCount;

		public readonly int InvalidEtag;
		public event Action<string, string, int, PresenceDocument> NotifyEvent;
		public event Action<string> SubscriptionRemovedEvent;

		public SimpleModule(IEqualityComparer<string> comparer)
		{
			int capacity = 16384;

			InvalidEtag = int.MinValue;

			publications = new ThreadSafe.Dictionary<string, Publication>(
				new Dictionary<string, Publication>(comparer));

			publicationTimer = new MultiTimer<PublicationExpiresKey>(
				PulicationExpired, capacity, true,
				new PublicationExpiresKeyEqualityComparer());

			subscriptionTimer = new MultiTimer<SubscriptionExpiresKey>(
				SubscriptionExpired, capacity, true,
				new SubscriptionExpiresKeyEqualityComparer());
		}

		public void Dispose()
		{
			publicationTimer.Dispose();
			subscriptionTimer.Dispose();
		}

		public bool Publish(string publisherId, ref int sipIfMatch, int expires, ArraySegment<byte> pidf)
		{
			bool result = true;

			bool addIfNotFound = expires > 0 && pidf.Count > 0;
			var publication = GetValidPublication(publisherId, addIfNotFound);

			if (publication != null)
			{
				if (expires > 0)
				{
					if (sipIfMatch == InvalidEtag)
						sipIfMatch = Interlocked.Increment(ref sipEtagCount);

					if (pidf.Count > 0)
						result = publication.Document.Modify(sipIfMatch, pidf);

					publicationTimer.Change(
						new PublicationExpiresKey(publisherId, sipIfMatch), expires * 1000);
				}
				else
				{
					if (sipIfMatch != InvalidEtag)
					{
						publication.Document.Remove(sipIfMatch);
						publicationTimer.Remove(new PublicationExpiresKey(publisherId, sipIfMatch));
					}
				}

				if (publication.Document.ResetChangedCount())
					OnPublicationChanged(publisherId, publication);
			}

			return result;
		}

		public PresenceDocument Subscribe(string subscriptionId, string publisherId, int expires)
		{
			PresenceDocument document;
			var exriresKey = new SubscriptionExpiresKey(publisherId, subscriptionId);

			if (expires > 0)
			{
				var publication = GetValidPublication(publisherId, true);
				var subscription = new Subscription(subscriptionId, expires);

				publication.Subscriptions.Replace(subscription);

				subscriptionTimer.Change(exriresKey, (expires + 10) * 1000);

				document = publication.Document;
			}
			else
			{
				var publication = GetValidPublication(publisherId, false);

				if (publication != null)
					if (publication.Subscriptions.RemoveFirst(subscriptionId, IsSubscriptionIdEquals))
						OnSubscriptionRemoved(subscriptionId);

				subscriptionTimer.Remove(exriresKey);

				document = null;
			}

			return document;
		}

		public PresenceDocument GetDocument(string publisherId)
		{
			var publication = GetValidPublication(publisherId, false);

			return (publication == null) ? null : publication.Document;
		}

		private Func<Subscription, string, bool> IsSubscriptionIdEquals = (subscription, id) =>
		{
			return subscription.Id.Equals(id);
		};

		private Publication GetValidPublication(string publicationId, bool addIfNotFound)
		{
			Publication publication;
			if (publications.TryGetValue(publicationId, out publication) == false)
			{
				if (addIfNotFound)
					publication = publications.GetOrAdd(publicationId, new Publication(publicationId));
			}

			return publication;
		}

		private void PulicationExpired(PublicationExpiresKey key)
		{
			Publication publication;
			if (publications.TryGetValue(key.Id, out publication))
			{
				publication.Document.Remove(key.SipEtag);

				if (publication.Document.ResetChangedCount())
					OnPublicationChanged(key.Id, publication);
			}
		}

		private void SubscriptionExpired(SubscriptionExpiresKey key)
		{
			Publication publication;
			if (publications.TryGetValue(key.PublicationId, out publication))
			{
				if (publication.Subscriptions.RemoveFirst(key.SubscriptionId, IsSubscriptionIdEquals))
				{
					OnSubscriptionRemoved(key.SubscriptionId);
					OnSubscriptionExpired(key.PublicationId, key.SubscriptionId);
				}
			}
		}

		private void OnPublicationChanged(string publisherId, Publication publication)
		{
			var handler = NotifyEvent;
			if (handler != null)
			{
				publication.Subscriptions.RecursiveForEach((subscription) =>
					handler(publisherId, subscription.Id, subscription.Expires, publication.Document));
			}
		}

		private void OnSubscriptionExpired(string publisherId, string supscriptionId)
		{
			var handler = NotifyEvent;
			if (handler != null)
				handler(publisherId, supscriptionId, 0, null);
		}

		private void OnSubscriptionRemoved(string subscriptionId)
		{
			var handler = SubscriptionRemovedEvent;
			if (handler != null)
				handler(subscriptionId);
		}
	}
}

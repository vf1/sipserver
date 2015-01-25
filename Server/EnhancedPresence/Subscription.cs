using System;
using System.Collections.Generic;

namespace EnhancedPresence
{
    public class Subscription :
        ISubscriptionResourceEvents
    {
		public static event Func<string, Publication> ResolvePublicationEvent;

		public string Uri { private set; get; }
		public string EndpointId { private set; get; }
		public string DialogId { private set; get; }
		public object Param { private set; get; }

        public Dictionary<string, SubscriptionResource> Resources { private set; get; }

        public Subscription(string uri, string endpointId, object param)
        {
            this.Uri = uri;
			this.EndpointId = endpointId;
			this.Param = param;

			Resources = new Dictionary<string, SubscriptionResource>();
		}

        public void AddResource(SubscriptionResource resource)
        {
            Resources.Add(resource.Uri, resource);
        }

        public void RemoveResource(SubscriptionResource resource)
        {
            Resources.Remove(resource.Uri);
        }

        public void UpdateResource(SubscriptionResource resource)
        {
			throw new NotSupportedException();
		}

		public void Process(BatchSubscribe batchSubs, ExpiresList expireList, uint expires, out Rlmi rlmi, out List<Categories> categories)
        {
			rlmi = null;
			categories = null;

			if (batchSubs == null && expires == 0)
			{
				List<SubscriptionResource> resources = new List<SubscriptionResource>(Resources.Values);

				foreach (SubscriptionResource resource in resources)
					resource.Destroy();
			}

			else if (batchSubs.Action == BatchSubscribeAction.Unsubscribe || expires == 0)
			{
				rlmi = Rlmi.Create(this.Uri);

				foreach (string uri in batchSubs.Resources)
				{
					SubscriptionResource resource;

					if (Resources.TryGetValue(uri, out resource))
					{
						resource.Destroy();
					}
				}
			}

			else if (batchSubs.Action == BatchSubscribeAction.Subscribe && expires > 0)
			{
				rlmi = Rlmi.Create(this.Uri);
				categories = new List<Categories>();

				foreach (string uri in batchSubs.Resources)
				{
					SubscriptionResource resource;
					if (Resources.TryGetValue(uri, out resource))
					{
						resource.SubsCategories = batchSubs.Сategories;
						resource.Expires = expires;
					}
					else
					{
						Publication publication = null;
						if (ResolvePublicationEvent != null)
							publication = ResolvePublicationEvent(uri);

						if (publication != null)
						{
							resource = new SubscriptionResource(this, publication, batchSubs.Сategories, expireList, expires);
						}
						else
						{
							rlmi.AddResubscribe(uri);
						}
					}

					if (resource != null)
					{
						categories.Add(
							resource.EndSubscribeProccessing()
						);
					}
				}
			}
        }
    }
}

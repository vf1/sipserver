using System;
using System.Collections.Generic;

namespace EnhancedPresence
{
    public class ExpiresList :
        ISubscriptionResourceEvents
    {
        private List<SubscriptionResource> resources;

        public ExpiresList()
        {
            this.resources = new List<SubscriptionResource>();
        }

        public void AddResource(SubscriptionResource resource)
        {
            int tickCount = Environment.TickCount;

            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].GetExpires(tickCount) > resource.GetExpires(tickCount))
                {
                    resources.Insert(i, resource);
                    break;
                }
            }
        }

        public void RemoveResource(SubscriptionResource resource)
        {
            resources.Remove(resource);
        }

        public void UpdateResource(SubscriptionResource resource)
        {
            RemoveResource(resource);
            AddResource(resource);
        }

        public SubscriptionResource GetExpired()
        {
			SubscriptionResource resource = null;
			
			if (resources.Count > 0)
			{
				if (resources[0].Expires == 0)
				{
					resource = resources[0];
					resources.RemoveAt(0);
				}
			}

            return resource;
        }
    }
}

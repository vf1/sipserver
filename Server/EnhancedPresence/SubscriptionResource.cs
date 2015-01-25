using System;
using System.Collections.Generic;

namespace EnhancedPresence
{
    public interface ISubscriptionResourceEvents
    {
        void AddResource(SubscriptionResource resource);
        void RemoveResource(SubscriptionResource resource);
        void UpdateResource(SubscriptionResource resource);
    }

    public class SubscriptionResource
    {
		public static event Action<SubscriptionResource, Categories>  NotifySubscriberEvent;

		private uint expires;
        private int tickCount;

        public readonly Subscription Subscription;
        public readonly Publication Publication;
        private readonly ExpiresList expiresList;
        public List<string> SubsCategories;
		private Categories updateCategories;
		private bool proccessingSubscribe;

		public SubscriptionResource(Subscription subscription, Publication publication, IEnumerable<string> categories, ExpiresList expiresList, uint expires)
        {
			this.proccessingSubscribe = true;
            this.Subscription = subscription;
            this.Publication = publication;
            this.SubsCategories = new List<string>(categories);
            this.expiresList = expiresList;
            this.expires = expires;
            this.tickCount = Environment.TickCount;

            this.Subscription.AddResource(this);
            this.Publication.AddResource(this);
            this.expiresList.AddResource(this);
        }

        public void Destroy()
        {
            this.expiresList.RemoveResource(this);
            this.Publication.RemoveResource(this);
            this.Subscription.RemoveResource(this);
        }

        public uint Expires
        {
            get
            {
                return GetExpires(Environment.TickCount);
            }
            set
            {
                this.expires = value;
                this.tickCount = Environment.TickCount;
                this.expiresList.UpdateResource(this);
            }
        }

        public uint GetExpires(int tickCount)
        {
            uint passed = (uint)((tickCount - this.tickCount) / 1000);

            return (expires > passed) ? expires - passed : 0;
        }

        public string Uri
        {
            get 
			{ 
				return this.Publication.Uri;
			}
        }

		public void PreNotifySubscriber(IEnumerable<Category> categories, string[] exclude)
		{
			foreach (var category in categories)
			{
				if (exclude == null || Array.Exists<string>(exclude, exclude1 => exclude1 == category.Name) == false)
					if (SubsCategories.Contains(category.Name))
					{
						if (updateCategories == null)
							updateCategories = Categories.Create(Uri);
						updateCategories.Items.Add(category);
					}
			}
		}

		public void NotifySubscriber()
		{
			if (this.proccessingSubscribe == false)
			{
				if (updateCategories != null)
				{
					SubscriptionResource.NotifySubscriberEvent(this, updateCategories);
					updateCategories = null;
				}
			}
		}

		public Categories EndSubscribeProccessing()
		{
			Categories result = updateCategories;
			if (result == null)
				result = Categories.Create(Uri);

			this.updateCategories = null;
			this.proccessingSubscribe = false;

			return result;
		}
    }
}

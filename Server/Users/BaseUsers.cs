using System;
using System.Collections.Generic;

namespace Sip.Server.Users
{
	public abstract class BaseUsers
		: IUsers
	{
		public event IUsersEventHandler2 Reset;
		public event IUsersEventHandler1 Added;
		public event IUsersEventHandler1 Updated;
		public event IUsersEventHandler1 Removed;

		public abstract void Dispose();
		public abstract string Id { get; }
		public abstract string SourceName { get; }
		public abstract bool HasPasswords { get; }
		public abstract bool IsReadOnly { get; }
		public abstract void Add(int accountId, IUser user);
		public abstract void Update(int accountId, IUser user);
		public abstract void Remove(int accountId, string username);
		public abstract int GetCount(int accountId);
		public abstract IList<IUser> GetUsers(int accountId, int startIndex, int count);
		public abstract IUser GetByName(int accountId, string username);

		protected void OnReset(int accountId)
		{
			var handler = Reset;
			if (handler != null)
				handler(accountId, this);
		}

		protected void OnAdded(int accountId, IUser user)
		{
			var handler = Added;
			if (handler != null)
				handler(accountId, this, user);
		}

		protected void OnUpdated(int accountId, IUser user)
		{
			var handler = Updated;
			if (handler != null)
				handler(accountId, this, user);
		}

		protected void OnRemoved(int accountId, IUser user)
		{
			var handler = Removed;
			if (handler != null)
				handler(accountId, this, user);
		}

		protected void OnError(string message)
		{
			//error = message;
		}

		protected static IList<Y> GetFromDictionary<X, Y>(IDictionary<string, X> dictionary, int startIndex, int count)
			where X : class
			where Y : class
		{
			if (dictionary != null && startIndex < dictionary.Count)
			{
				int available = dictionary.Count - startIndex;
				count = (available < count) ? available : count;

				var result = new Y[count];

				int i = 0;
				foreach (var item in dictionary.Values)
				{
					if (startIndex > 0)
						startIndex--;
					else
					{
						if (i >= count)
							break;

						result[i++] = item as Y;
					}
				}

				return result;
			}

			return null;
		}
	}
}

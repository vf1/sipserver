using System;
using System.Collections.Generic;

namespace Sip.Server.Users
{
	public delegate void IUsersEventHandler1(int accountId, IUsers source, IUser user);
	public delegate void IUsersEventHandler2(int accountId, IUsers source);

	public interface IUsers
		: IDisposable
	{
		event IUsersEventHandler2 Reset;
		event IUsersEventHandler1 Added;
		event IUsersEventHandler1 Updated;
		event IUsersEventHandler1 Removed;

		string Id { get; }
		string SourceName { get; }
		bool HasPasswords { get; }

		bool IsReadOnly { get; }

		void Add(int accountId, IUser user);
		void Update(int accountId, IUser user);
		void Remove(int accountId, string name);

		int GetCount(int accountId);

		IList<IUser> GetUsers(int accountId, int startIndex, int count);

		IUser GetByName(int accountId, string name);
	}
}

using System;

namespace Sip.Server.Users
{
	public interface IUserz
	{
		event IUsersEventHandler2 Reset;
		event IUsersEventHandler1 Added;
		event IUsersEventHandler1 Updated;
		event IUsersEventHandler1 Removed;

		IUsers this[int index] { get; }
		int Count { get; }

		int GetIndex(string id);
		IUsers Get(string id);

		IUsers[] ToArray();
	}
}

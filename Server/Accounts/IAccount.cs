using System;

namespace Sip.Server.Accounts
{
	public interface IAccount
	{
		int Id { get; }
		string Email { get; }
		string Password { get; }
		string DomainName { get; }
	}
}

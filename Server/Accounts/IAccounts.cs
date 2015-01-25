using System;
using Base.Message;

namespace Sip.Server.Accounts
{
	interface IAccounts
	{
		IAccount GetAccount(int id);
		IAccount GetAccount(ByteArrayPart domain);
		bool HasDomain(ByteArrayPart domain);

		void ForEach(Action<IAccount> action);
	}
}

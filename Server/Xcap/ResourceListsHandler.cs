using System;
using System.Collections.Generic;
using Sip.Server.Users;
using Sip.Server.Accounts;
using Base.Message;

namespace Server.Xcap
{
	class ResourceListsHandler
		: BaseResourceListsHandler
	{
		private readonly IUserz userz;
		private readonly IAccounts accounts;

		public ResourceListsHandler(IAccounts accounts, IUserz userz)
		{
			this.accounts = accounts;
			this.userz = userz;
		}

		protected override IEnumerable<Entry> GetEntries(ByteArrayPart username, ByteArrayPart domain)
		{
			var account = accounts.GetAccount(domain);

			if (account != null)
			{
				for (int i = 0; i < userz.Count; i++)
				{
					int count = userz[i].GetCount(account.Id);
					var users = userz[i].GetUsers(account.Id, 0, count);

					for (int j = 0; j < users.Count; j++)
					{
						var user = users[j];

						yield return new Entry("sip:" + user.Name + "@" + account.DomainName, user.DisplayName);
					}
				}
			}
		}
	}
}

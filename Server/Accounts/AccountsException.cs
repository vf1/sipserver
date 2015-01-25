using System;

namespace Sip.Server.Accounts
{
	enum AccountsErrors
	{
		InvalidId,
		DomainUsed,
		IdUsed,
		OldNotFound,
		DomainRequred,
	}

	public class AccountsException
		: Exception
	{
		internal AccountsException(AccountsErrors code)
			: base(GetMessage(code))
		{

		}

		internal static string GetMessage(AccountsErrors code)
		{
			switch (code)
			{
				case AccountsErrors.InvalidId:
					return @"account.Id == Account.InvalidId";
				case AccountsErrors.DomainUsed:
					return @"Domain name is used by another account";
				case AccountsErrors.IdUsed:
					return @"Failed to add account, id was used";
				case AccountsErrors.OldNotFound:
					return @"Old account not found";
				case AccountsErrors.DomainRequred:
					return @"Domain name is not specified";
			}

			return null;
		}
	}
}

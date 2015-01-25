using System;

namespace Sip.Server.Users
{
	enum ErrorCodes
	{
		UserExist,
		UsernameEmpty,
	}

	public class UsersException
		: Exception
	{
		internal UsersException(ErrorCodes code)
			: base(GetMessage(code))
		{

		}

		internal static string GetMessage(ErrorCodes code)
		{
			switch (code)
			{
				case ErrorCodes.UserExist:
					return @"User with specified username already exist";
				case ErrorCodes.UsernameEmpty:
					return @"Empty username is not allowed";
			}

			return null;
		}
	}
}

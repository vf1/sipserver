using System;
using System.Collections.Generic;
using System.DirectoryServices;
using Sip.Server.Tools;

namespace Sip.Server.Users
{
	class AdUser
		: BaseUser
	{
		private readonly static string[] properties = new string[]
		{
			@"sAMAccountName",
			@"memberOf",
			@"displayName",
			@"mail",
			@"telephoneNumber",
			@"facsimileTelephoneNumber",
			@"streetAddress",
			@"l",
			@"st",
			@"countryCode",
			@"postalCode",
			@"wWWHomePage",
			@"title",
			@"company",
			@"physicalDeliveryOfficeName"
		};

		public AdUser()
		{
		}

		public AdUser(SearchResult user)
		{
			Name = GetAsString(user, @"sAMAccountName").ToLower();
			DisplayName = GetAsString(user, @"displayName");
			Email = GetAsString(user, @"mail");
			Telephone = GetAsString(user, @"telephoneNumber");
			Fax = GetAsString(user, @"facsimileTelephoneNumber");
			StreetAddress = GetAsString(user, @"streetAddress");
			City = GetAsString(user, @"l");
			State = GetAsString(user, @"st");
			CountryCode = ISO3166.ToA2(GetAsInt(user, @"countryCode"), null);
			PostalCode = GetAsString(user, @"postalCode");
			WwwHomepage = GetAsString(user, @"wWWHomePage");
			Title = GetAsString(user, @"title");
			Company = GetAsString(user, @"company");
			PhysicalDeliveryOfficeName = GetAsString(user, @"physicalDeliveryOfficeName");
		}

		public static string[] Properties
		{
			get { return properties; }
		}

		private string GetAsString(SearchResult result, string name)
		{
			var s = String.Empty;

			try
			{
				s = result.Properties[name][0] as string;
			}
			catch
			{
			}

			return s;
		}


		private int GetAsInt(SearchResult result, string name)
		{
			int i = 0;

			try
			{
				i = (int)result.Properties[name][0];
			}
			catch
			{
			}

			return i;
		}
	}
}

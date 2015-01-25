using System;
using System.IO;
using LumenWorks.Framework.IO.Csv;

namespace Sip.Server.Users
{
	class CsvUser
		: BaseUser
	{
		public CsvUser()
		{
		}

		public CsvUser(IUser user)
		{
			CopyFrom(user);
		}

		public bool Read(CsvReader csv)
		{
			Name = GetField(csv, 0);

			Password = GetField(csv, 1);

			DisplayName = GetField(csv, 2);
			Email = GetField(csv, 3);

			Telephone = GetField(csv, 4);
			Fax = GetField(csv, 5);

			City = GetField(csv, 6);
			State = GetField(csv, 7);
			StreetAddress = GetField(csv, 8);
			CountryCode = GetField(csv, 9);
			PostalCode = GetField(csv, 10);
			WwwHomepage = GetField(csv, 11);
			Title = GetField(csv, 12);
			Company = GetField(csv, 13);
			PhysicalDeliveryOfficeName = GetField(csv, 14);

			if (string.IsNullOrEmpty(Name))
				return false;

			return true;
		}

		public override string ToString()
		{
			return ToString(this);
		}

		public static string ToString(IUser user)
		{
			return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
				QuoteIfReqired(user.Name),
				QuoteIfReqired(user.Password),
				QuoteIfReqired(user.DisplayName),
				QuoteIfReqired(user.Email),
				QuoteIfReqired(user.Telephone),
				QuoteIfReqired(user.Fax),
				QuoteIfReqired(user.City),
				QuoteIfReqired(user.State),
				QuoteIfReqired(user.StreetAddress),
				QuoteIfReqired(user.CountryCode),
				QuoteIfReqired(user.PostalCode),
				QuoteIfReqired(user.WwwHomepage),
				QuoteIfReqired(user.Title),
				QuoteIfReqired(user.Company),
				QuoteIfReqired(user.PhysicalDeliveryOfficeName));
		}

		public static string GetFormatDescription()
		{
			return
				"Name, " +
				"Password, " +
				"DisplayName, " +
				"Email, " +
				"Telephone, " +
				"Fax, " +
				"City, " +
				"State, " +
				"StreetAddress, " +
				"CountryCode, " +
				"PostalCode, " +
				"WwwHomepage, " +
				"Title, " +
				"Company, " +
				"PhysicalDeliveryOfficeName";
		}

		private static string QuoteIfReqired(string filed)
		{
			if (filed == null)
				return string.Empty;

			if (filed.IndexOfAny(new[] { '"' }) > 0)
				filed = "\"" + filed.Replace("\"", "\"\"") + "\"";

			return filed;
		}

		private string GetField(CsvReader csv, int index)
		{
			if (index < csv.FieldCount)
				return csv[index];
			return string.Empty;
		}
	}
}

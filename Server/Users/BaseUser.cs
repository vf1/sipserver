using System;

namespace Sip.Server.Users
{
	[Serializable]
	public class BaseUser
		: IUser, IEquatable<IUser>
	{
		public string Name { get; set; }
		//public int AccountId { get; set; }

		public string DisplayName { get; set; }
		public string Email { get; set; }

		public string Password { get; set; }

		public string Telephone { get; set; }
		public string Fax { get; set; }

		public string City { get; set; }
		public string State { get; set; }
		public string StreetAddress { get; set; }
		public string CountryCode { get; set; }
		public string PostalCode { get; set; }
		public string WwwHomepage { get; set; }
		public string Title { get; set; }
		public string Company { get; set; }
		public string PhysicalDeliveryOfficeName { get; set; }

		public void CopyFrom(IUser user)
		{
			Name = user.Name;
			//AccountId = user.AccountId;

			DisplayName = user.DisplayName;
			Email = user.Email;

			Password = user.Password;

			Telephone = user.Telephone;
			Fax = user.Fax;

			City = user.City;
			State = user.State;
			StreetAddress = user.StreetAddress;
			CountryCode = user.CountryCode;
			PostalCode = user.PostalCode;
			WwwHomepage = user.WwwHomepage;
			Title = user.Title;
			Company = user.Company;
			PhysicalDeliveryOfficeName = user.PhysicalDeliveryOfficeName;
		}

		public bool Equals(IUser other)
		{
			return
				Name == other.Name &&
				//AccountId == other.AccountId &&
				DisplayName == other.DisplayName &&
				Email == other.Email &&
				Password == other.Password &&
				Telephone == other.Telephone &&
				Fax == other.Fax &&
				City == other.City &&
				State == other.State &&
				StreetAddress == other.StreetAddress &&
				CountryCode == other.CountryCode &&
				PostalCode == other.PostalCode &&
				WwwHomepage == other.WwwHomepage &&
				Title == other.Title &&
				Company == other.Company &&
				PhysicalDeliveryOfficeName == other.PhysicalDeliveryOfficeName;
		}
	}
}

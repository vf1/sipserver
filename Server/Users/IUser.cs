using System;

namespace Sip.Server.Users
{
	public interface IUser
	{
		string Name { get; }

		string DisplayName { get; set; }
		string Email { get; set; }

		string Password { get; set; }

		string Telephone { get; set; }
		string Fax { get; set; }

		string City { get; set; }
		string State { get; set; }
		string StreetAddress { get; set; }
		string CountryCode { get; set; }
		string PostalCode { get; set; }
		string WwwHomepage { get; set; }
		string Title { get; set; }
		string Company { get; set; }
		string PhysicalDeliveryOfficeName { get; set; }
	}
}

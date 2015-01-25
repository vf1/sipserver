using System.Runtime.Serialization;
using Sip.Server.Users;

namespace Sip.Server.WcfService
{
	[DataContract(Name = "User", Namespace = "http://officesip.com/server.control")]
	public class WcfUser
	{
		public WcfUser(IUser user)
		{
			Name = user.Name;
			DisplayName = user.DisplayName;
			Email = user.Email;
		}

		[DataMember]
		public string Name { set; get; }
		[DataMember]
		public string DisplayName { set; get; }
		[DataMember]
		public string Email { set; get; }
		[DataMember]
		public int Availability { set; get; }

		public IUser ToIUser(string password)
		{
			return new BaseUser()
			{
				Name = Name,
				DisplayName = DisplayName,
				Email = Email,
				Password = password,
			};
		}
	}
}

using System.Runtime.Serialization;
using Sip.Server.Users;

namespace Sip.Server.WcfService
{
	[DataContract(Name = "Users", Namespace = "http://officesip.com/server.control")]
	public class WcfUsers
	{
		public WcfUsers(IUsers users)
		{
			Id = users.Id;
			SourceName = users.SourceName;
			IsReadOnly = users.IsReadOnly;
		}

		[DataMember]
		public string Id { get; private set; }
		[DataMember]
		public string SourceName { get; private set; }
		[DataMember]
		public bool IsReadOnly { get; set; }
	}
}

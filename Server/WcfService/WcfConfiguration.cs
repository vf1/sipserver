using System.Net;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Sip.Server.Users;

namespace Sip.Server.WcfService
{
	[DataContract(Name = "Configurations", Namespace = "http://officesip.com/server.control")]
	public class WcfConfiguration
	{
		[DataMember]
		public string DomainName { set; get; }



		[DataMember]
		public bool IsAuthorizationEnabled { set; get; }




		[DataMember]
		public bool IsActiveDirectoryUsersEnabled { set; get; }

		[DataMember]
		public string ActiveDirectoryUsersGroup { set; get; }



		[DataMember]
		public bool IsTracingEnabled { set; get; }

		[DataMember]
		public string TracingFileName { set; get; }



		[DataMember]
		public IEnumerable<WcfUsers> Users { get; set; }


		//public IPEndPoint PortForwardingPublicIPEndPoint
		//public IPEndPoint PortForwardingPrivateIPEndPoint
	}
}

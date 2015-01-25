using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Sip.Server.WcfService
{
	[DataContract(Name = "TURNConfigurations", Namespace = "http://officesip.com/server.control")]
	public class WcfTurnConfiguration
	{
		[DataMember]
		public byte[] Key1 { get; set; }
		[DataMember]
		public byte[] Key2 { get; set; }
		[DataMember]
		public string FQDN { get; set; }
		[DataMember]
		public int UDPPort { get; set; }
		[DataMember]
		public int TCPPort { get; set; }
	}
}

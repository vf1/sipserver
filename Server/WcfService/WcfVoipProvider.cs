using System.Net;
using System.Runtime.Serialization;
using Sip.Server.Users;

namespace Sip.Server.WcfService
{
	[DataContract(Name = "VoipProvider", Namespace = "http://officesip.com/server.control")]
	public class WcfVoipProvider
	{
		public WcfVoipProvider()
		{
		}

		public WcfVoipProvider(Sip.Server.Trunk trunk)
		{
			Hostname = trunk.Domain.ToString();
			Username = trunk.Username;
			ErrorMessage = trunk.ErrorMessage;
		}

		public WcfVoipProvider(Sip.Server.Configuration.VoipProviderConfigurationElement configElem, Sip.Server.Trunk trunk)
		{
			AuthenticationId = configElem.AuthenticationId.ToString();
			DisplayName = configElem.DisplayName.ToString();
			ForwardCallTo = configElem.ForwardIncomingCallTo.Substring(
				configElem.ForwardIncomingCallTo.StartsWith("sip:") ? 4 : 0);
			LocalEndPoint = configElem.LocalEndpoint;
			OutgoingProxy = configElem.OutboundProxyHostname;
			Password = configElem.Password;
			Transport = configElem.Protocol.ToString();
			Hostname = configElem.ServerHostname;
			Username = configElem.Username;

			if (trunk != null)
				ErrorMessage = trunk.ErrorMessage;
			else
				ErrorMessage = @"Loading...";
		}

		public Sip.Server.Configuration.VoipProviderConfigurationElement ToVoipProviderConfigurationElement()
		{
			return new Sip.Server.Configuration.VoipProviderConfigurationElement()
			{
				AuthenticationId = this.AuthenticationId,
				DisplayName = this.DisplayName,
				ForwardIncomingCallTo = this.ForwardCallTo.StartsWith("sip:") ? this.ForwardCallTo : "sip:" + this.ForwardCallTo,
				LocalEndpoint = this.LocalEndPoint,
				OutboundProxyHostname = this.OutgoingProxy,
				Password = this.Password,
				Protocol = (string.Compare(this.Transport, @"udp", true) == 0)
					? SocketServers.ServerProtocol.Udp : SocketServers.ServerProtocol.Tcp,
				ServerHostname = this.Hostname,
				Username = this.Username,
			};
		}

		[DataMember]
		public string DisplayName { get; set; }
		[DataMember]
		public string Hostname { get; set; }
		[DataMember]
		public string Username { get; set; }
		[DataMember]
		public string Transport { get; set; }
		[DataMember]
		public IPEndPoint LocalEndPoint { get; set; }
		[DataMember]
		public string OutgoingProxy { get; set; }
		[DataMember]
		public string AuthenticationId { get; set; }
		[DataMember]
		public string Password { get; set; }
		[DataMember]
		public string ForwardCallTo { get; set; }
		[DataMember]
		public string ErrorMessage { get; set; }
	}
}

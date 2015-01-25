using System;
using System.Configuration;

namespace Sip.Server.Configuration
{
	public class TurnServerConfigurationElement
		: ConfigurationElement
	{
		private const string fqdnName = @"fqdn";
		private const string udpPortName = @"udpPort";
		private const string tcpPortName = @"tcpPort";
		private const string locationName = @"location";

		[ConfigurationProperty(fqdnName, IsRequired = true)]
		public string Fqdn
		{
			get { return (string)base[fqdnName]; }
			set { base[fqdnName] = value; }
		}

		[ConfigurationProperty(udpPortName, DefaultValue = 3478)]
		[IntegerValidator(MinValue = 0, MaxValue = 65535)]
		public int UdpPort
		{
			get { return (int)base[udpPortName]; }
			set { base[udpPortName] = value; }
		}

		[ConfigurationProperty(tcpPortName, DefaultValue = 3478)]
		[IntegerValidator(MinValue = 0, MaxValue = 65535)]
		public int TcpPort
		{
			get { return (int)base[tcpPortName]; }
			set { base[tcpPortName] = value; }
		}

		[ConfigurationProperty(locationName, DefaultValue = "Internet")]
		[RegexStringValidator("Internet|Intranet")]
		public string Location
		{
			get { return (string)base[locationName]; }
			set { base[locationName] = value; }
		}

		public bool IsInternet
		{
			get { return string.Compare(Location, "Internet") == 0; }
			set { Location = "Internet"; }
		}
	}
}

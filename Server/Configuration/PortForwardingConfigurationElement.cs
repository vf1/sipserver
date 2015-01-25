using System;
using System.Net;
using System.Configuration;
using System.ComponentModel;
using SocketServers;

namespace Sip.Server.Configuration
{
	public class PortForwardingConfigurationElement
		: ConfigurationElement
	{
		private const string protocolName = @"protocol";
		private const string localEndpointName = @"localEndpoint";
		private const string externalEndpointName = @"externalEndpoint";

		[ConfigurationProperty(protocolName, IsRequired = true)]
		[TypeConverter(typeof(ProtocolConverter))]
		public ServerProtocol Protocol
		{
			get { return (ServerProtocol)base[protocolName]; }
			set { base[protocolName] = value; }
		}

		[ConfigurationProperty(localEndpointName, IsRequired = true)]
		[TypeConverter(typeof(IPEndPointConverter))]
		public IPEndPoint LocalEndpoint
		{
			get { return (IPEndPoint)base[localEndpointName]; }
			set { base[localEndpointName] = value; }
		}

		[ConfigurationProperty(externalEndpointName, IsRequired = true)]
		[TypeConverter(typeof(IPEndPointConverter))]
		public IPEndPoint ExternalEndpoint
		{
			get { return (IPEndPoint)base[externalEndpointName]; }
			set { base[externalEndpointName] = value; }
		}
	}
}

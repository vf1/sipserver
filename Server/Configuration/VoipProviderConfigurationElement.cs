using System;
using System.Net;
using System.Configuration;
using System.ComponentModel;
using SocketServers;

namespace Sip.Server.Configuration
{
	public class VoipProviderConfigurationElement
		: ConfigurationElement
	{
		private const string serverHostnameName = @"serverHostname";
		private const string outboundProxyHostnameName = @"outboundProxyHostname";
		private const string protocolName = @"protocol";
		private const string localEndpointName = @"localEndpoint";
		private const string usernameName = @"username";
		private const string displayNameName = @"displayName";
		private const string authenticationIdName = @"authenticationId";
		private const string passwordName = @"password";
		private const string forwardIncomingCallToName = @"forwardIncomingCallTo";
		private const string restoreAfterErrorTimeoutName = @"restoreAfterErrorTimeout";

		[ConfigurationProperty(serverHostnameName, IsRequired = true)]
		public string ServerHostname
		{
			get { return (string)base[serverHostnameName]; }
			set { base[serverHostnameName] = value; }
		}

		[ConfigurationProperty(outboundProxyHostnameName)]
		public string OutboundProxyHostname
		{
			get { return (string)base[outboundProxyHostnameName]; }
			set { base[outboundProxyHostnameName] = value; }
		}

		[ConfigurationProperty(protocolName, IsRequired = true)]
		[TypeConverter(typeof(ProtocolConverter))]
		public ServerProtocol Protocol
		{
			get { return (ServerProtocol)base[protocolName]; }
			set { base[protocolName] = value; }
		}

		[ConfigurationProperty(localEndpointName)]
		[TypeConverter(typeof(IPEndPointConverter))]
		public IPEndPoint LocalEndpoint
		{
			get { return (IPEndPoint)base[localEndpointName]; }
			set { base[localEndpointName] = value; }
		}

		[ConfigurationProperty(usernameName, IsRequired = true)]
		public string Username
		{
			get { return (string)base[usernameName]; }
			set { base[usernameName] = value; }
		}

		[ConfigurationProperty(displayNameName)]
		public string DisplayName
		{
			get { return (string)base[displayNameName]; }
			set { base[displayNameName] = value; }
		}

		[ConfigurationProperty(authenticationIdName)]
		public string AuthenticationId
		{
			get { return (string)base[authenticationIdName]; }
			set { base[authenticationIdName] = value; }
		}

		[ConfigurationProperty(passwordName)]
		public string Password
		{
			get { return (string)base[passwordName]; }
			set { base[passwordName] = value; }
		}

		[ConfigurationProperty(forwardIncomingCallToName)]
		public string ForwardIncomingCallTo
		{
			get { return (string)base[forwardIncomingCallToName]; }
			set { base[forwardIncomingCallToName] = value; }
		}

		[ConfigurationProperty(restoreAfterErrorTimeoutName, DefaultValue = 60)]
		[IntegerValidator(MinValue = 0, MaxValue = int.MaxValue)]
		public int RestoreAfterErrorTimeout
		{
			get { return (int)base[restoreAfterErrorTimeoutName]; }
			set { base[restoreAfterErrorTimeoutName] = value; }
		}
	}
}

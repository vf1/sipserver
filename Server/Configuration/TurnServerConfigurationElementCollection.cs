using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;

namespace Sip.Server.Configuration
{
	public class TurnServerConfigurationElementCollection
		: ConfigurationElementCollection, IEnumerable<TurnServerConfigurationElement>
	{
		private const string key1Name = @"key1";
		private const string key2Name = @"key2";

		protected override ConfigurationElement CreateNewElement()
		{
			return new TurnServerConfigurationElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			var turnServer = element as TurnServerConfigurationElement;
			return turnServer.Location + "@" + turnServer.Fqdn;
		}

		[ConfigurationProperty(key1Name)]
		[TypeConverter(typeof(ByteArrayConverter))]
		public byte[] Key1
		{
			get { return (byte[])base[key1Name]; }
			set { base[key1Name] = value; }
		}

		[ConfigurationProperty(key2Name)]
		[TypeConverter(typeof(ByteArrayConverter))]
		public byte[] Key2
		{
			get { return (byte[])base[key2Name]; }
			set { base[key2Name] = value; }
		}

		public void Clear()
		{
			BaseClear();
		}

		public void Add(string fqdn, int tcpPort, int udpPort, bool isInternet)
		{
			BaseAdd(new TurnServerConfigurationElement()
				{
					Fqdn = fqdn,
					TcpPort = tcpPort,
					UdpPort = udpPort,
					IsInternet = isInternet,
				});
		}

		public new IEnumerator<TurnServerConfigurationElement> GetEnumerator()
		{
			return new TypedEnumerator<TurnServerConfigurationElement>(base.GetEnumerator());
		}
	}
}

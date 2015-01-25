using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;

namespace Sip.Server.Configuration
{
	public class PortForwardingConfigurationElementCollection
		: ConfigurationElementCollection, IEnumerable<PortForwardingConfigurationElement>
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new PortForwardingConfigurationElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			var forwarding = element as PortForwardingConfigurationElement;
			return forwarding.Protocol.ToString() + ":" + forwarding.LocalEndpoint.ToString();
		}

		public new IEnumerator<PortForwardingConfigurationElement> GetEnumerator()
		{
			return new TypedEnumerator<PortForwardingConfigurationElement>(base.GetEnumerator());
		}
	}
}

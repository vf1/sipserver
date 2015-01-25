using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace Sip.Server.Configuration
{
	class VoipProviderConfigurationElementCollection
		: ConfigurationElementCollection, IEnumerable<VoipProviderConfigurationElement>
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new VoipProviderConfigurationElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			var voipProvider = element as VoipProviderConfigurationElement;
			return voipProvider.Username + "@" + voipProvider.ServerHostname;
		}

		public void Clear()
		{
			BaseClear();
		}

		public void Add(VoipProviderConfigurationElement item)
		{
			BaseAdd(item);
		}

		public void Remove(string username, string hostname)
		{
			BaseRemove(username + "@" + hostname);
		}

		public new IEnumerator<VoipProviderConfigurationElement> GetEnumerator()
		{
			return new TypedEnumerator<VoipProviderConfigurationElement>(base.GetEnumerator());
		}
	}
}

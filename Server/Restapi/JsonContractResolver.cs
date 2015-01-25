using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sip.Server.Users;
using Sip.Server.Accounts;

namespace Server.Restapi
{
	public class JsonContractResolver : CamelCasePropertyNamesContractResolver
	{
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var properties = base.CreateProperties(type, memberSerialization);

			//if (type == typeof(Account) || type.BaseType == typeof(BaseUser))
			{
				for (int i = properties.Count - 1; i >= 0; i--)
				{
					if (properties[i].PropertyName == "password")
						properties.RemoveAt(i);
				}
			}

			return properties;
		}
	}
}

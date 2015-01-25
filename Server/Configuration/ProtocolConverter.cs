using System;
using System.Configuration;
using System.ComponentModel;
using System.Globalization;
using SocketServers;

namespace Sip.Server.Configuration
{
	class ProtocolConverter
		: ConfigurationConverterBase
	{
		public override bool CanConvertTo(ITypeDescriptorContext ctx, Type type)
		{
			return type == typeof(string);
		}

		public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type type)
		{
			return type == typeof(string);
		}

		public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
		{
			if (value == null)
				return new ArgumentNullException();

			if (value.GetType() != typeof(ServerProtocol))
				throw new ArgumentException();

			return value.ToString();
		}

		public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
		{
			if (data == null)
				return new ArgumentNullException(@"data");

			return (data as string).ConvertTo();
		}
	}
}

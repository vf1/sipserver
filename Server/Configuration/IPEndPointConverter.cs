using System;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Sip.Server.Configuration
{
	class IPEndPointConverter
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
				return new ArgumentNullException(@"value");

			var endpoint = value as IPEndPoint;

			if (endpoint == null)
				throw new ArgumentException(@"value");

			if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
				return @"[" + endpoint.Address.ToString() + @"]:" + endpoint.Port;

			return endpoint.ToString();
		}

		public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
		{
			if (data == null)
				throw new ArgumentOutOfRangeException(@"IP End Point");

			var regex = new Regex(@"^(((\[(?<address>[^\]]+)\])|(?<address>[^:]+)):(?<port>[0-9]+))",
				RegexOptions.Singleline);

			var match = regex.Match(data as string);

			if (match.Success == false)
				throw new ArgumentOutOfRangeException(@"IP End Point");

			IPAddress address;
			if (IPAddress.TryParse(match.Groups["address"].Value, out address) == false)
				throw new ArgumentOutOfRangeException(@"IP Address");

			int port;
			if (int.TryParse(match.Groups["port"].Value, out port) == false)
				throw new ArgumentOutOfRangeException(@"Port");

			if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
				return null;

			return new IPEndPoint(address, port);
		}
	}
}

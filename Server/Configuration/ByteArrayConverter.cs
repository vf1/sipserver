using System;
using System.Configuration;
using System.ComponentModel;
using System.Globalization;

namespace Sip.Server.Configuration
{
	class ByteArrayConverter
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

			if (value.GetType() != typeof(byte[]))
				throw new ArgumentException();

			return Convert.ToBase64String(value as byte[]);
		}

		public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
		{
			if (data == null)
				return new ArgumentNullException(@"data");

			return Convert.FromBase64String(data as string);
		}
	}
}

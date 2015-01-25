using System;
using System.Text;
using System.Globalization;
using System.Windows.Controls;
using System.Net;

namespace ControlPanel
{
	public class IPEndPointValidationRule
		: ValidationRule
	{
		public override ValidationResult Validate(object value1, CultureInfo cultureInfo)
		{
			var value = value1 as string;

			int hcolon = value.IndexOf(':');
			if (hcolon >= 0)
			{
				int port;
				if (int.TryParse(value.Substring(hcolon + 1), out port) == false || port > 65535 || port < 0)
					return new ValidationResult(false, "Invalid port number");

				value = value.Substring(0, hcolon);
			}

			IPAddress ip;
			if (IPAddress.TryParse(value as string, out ip) == false)
				return new ValidationResult(false, "Invalid IP address");

			return new ValidationResult(true, null);
		}
	}
}

using System;
using System.Text;
using System.Globalization;
using System.Windows.Controls;
using System.Net;

namespace ControlPanel
{
	public class IPAddressValidationRule
		: ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			IPAddress ip;
			if (IPAddress.TryParse(value as string, out ip))
				return new ValidationResult(true, null);

			return new ValidationResult(false, "Invalid IP address");
		}
	}
}

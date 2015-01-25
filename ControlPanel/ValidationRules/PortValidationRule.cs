using System;
using System.Text;
using System.Globalization;
using System.Windows.Controls;

namespace ControlPanel
{
	public class PortValidationRule
		: ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			int port;
			if (int.TryParse(value as string, out port))
			{
				if (port > 0 && port <= 65535)
					return new ValidationResult(true, null);
			}

			return new ValidationResult(false, "Port in integer value from 0 to 65535");
		}
	}
}

using System;
using System.Text;
using System.Globalization;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace ControlPanel
{
	public class UsernameValidationRule
		: ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			Regex regex = new Regex(@"^[a-z0-9\.\-_]+$");

			if(regex.IsMatch(value as string))
				return new ValidationResult(true, null);

			return new ValidationResult(false, "Invalid username");
		}
	}
}

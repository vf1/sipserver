using System;
using System.Text;
using System.Globalization;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace ControlPanel
{
	public class DisplayNameValidationRule
		: ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			Regex regex = new Regex("^[^\"]*$");

			if (regex.IsMatch(value as string))
				return new ValidationResult(true, null);

			return new ValidationResult(false, "Invalid simbols");
		}
	}
}

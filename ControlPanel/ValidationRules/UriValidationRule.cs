using System;
using System.Text;
using System.Globalization;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace ControlPanel
{
	public class UriValidationRule
		: ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			Regex regex = new Regex(
				@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
				@"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
				@".)+))([a-zA-Z0-9]+)(\]?)$");

			if (regex.IsMatch(value as string))
				return new ValidationResult(true, null);

			return new ValidationResult(false, "Invalid URI");
		}
	}
}

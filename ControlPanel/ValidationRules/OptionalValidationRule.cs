using System;
using System.Text;
using System.Globalization;
using System.Windows.Controls;
using System.Net;

namespace ControlPanel
{
	public class OptionalValidationRule
		: ValidationRule
	{
		private ValidationRule rule;

		public OptionalValidationRule(ValidationRule rule)
		{
			this.rule = rule;
		}

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (string.IsNullOrEmpty(value as string) == false)
				return rule.Validate(value, cultureInfo);

			return new ValidationResult(true, null);
		}
	}
}

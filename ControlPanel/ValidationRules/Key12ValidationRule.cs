using System;
using System.Text;
using System.Globalization;
using System.Windows.Controls;

namespace ControlPanel
{
	public class Key12ValidationRule
		: ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			try
			{
				byte[] key = Convert.FromBase64String(value as string);
				if (key.Length != 20)
					throw new Exception();
			}
			catch
			{
				return new ValidationResult(false, "Invalid key, it must base64 encoded value 20 bytes length");
			}

			return new ValidationResult(true, null);
		}
	}
}

using System;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Controls;

namespace ControlPanel
{
	public class Validator : MarkupExtension
	{
		Binding binding;

		public Validator()
		{
			binding = new Binding(@"None");
			binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
			binding.Mode = BindingMode.OneWayToSource;
			binding.Source = this;
			binding.NotifyOnValidationError = true;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return binding.ProvideValue(serviceProvider);
		}

		public string Rule
		{
			get
			{
				return binding.ValidationRules[0].ToString();
			}
			set
			{
				binding.ValidationRules.Clear();

				if (value.StartsWith("~"))
					binding.ValidationRules.Add(new OptionalValidationRule(GetValidationRule(value.Substring(1))));
				else
					binding.ValidationRules.Add(GetValidationRule(value));
			}
		}

		private ValidationRule GetValidationRule(string value)
		{
			switch (value)
			{
				case "UsernameValidationRule":
					return new UsernameValidationRule();
				case "DisplayNameValidationRule":
					return new DisplayNameValidationRule();
				case "EmailValidationRule":
					return new EmailValidationRule();
				case "PortValidationRule":
					return new PortValidationRule();
				case "Key12ValidationRule":
					return new Key12ValidationRule();
				case "IPAddressValidationRule":
					return new IPAddressValidationRule();
				case "IPEndPointValidationRule":
					return new IPEndPointValidationRule();
				case "HostnameValidationRule":
					return new HostnameValidationRule();
				case "UriValidationRule":
					return new UriValidationRule();
				default:
					throw new NotSupportedException();
			}
		}

		// Do NOT remove!!!
		public object None
		{
			get;
			set;
		}
	}
}

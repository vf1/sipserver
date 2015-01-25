using System;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Globalization;

namespace ControlPanel
{
	[ValueConversion(typeof(string[]), typeof(Visibility))]
	[ValueConversion(typeof(string), typeof(Visibility))]
	[ValueConversion(typeof(bool), typeof(Visibility))]
	class VisibilityConverter
		: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return Visibility.Collapsed;
			if (value is string[])
				if ((value as string[]).Length == 0)
					return Visibility.Collapsed;
			if (value is bool)
				if ((bool)value == false)
					return Visibility.Collapsed;
			return Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}

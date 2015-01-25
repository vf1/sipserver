using System;
using System.Windows.Data;
using System.Globalization;

namespace ControlPanel
{
	[ValueConversion(typeof(bool), typeof(bool))]
	public class TrueFalseConverter
		: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !((bool)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !((bool)value);
		}
	}
}

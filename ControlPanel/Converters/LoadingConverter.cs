using System;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Globalization;

namespace ControlPanel
{
	[ValueConversion(typeof(Service1.User), typeof(string))]
	class LoadingConverter
		: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var user = value as Service1.User;

			if (user == null)
				return @"Loading...";

			return user.Name;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}

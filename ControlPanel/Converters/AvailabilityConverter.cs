using System;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Media;

namespace ControlPanel
{
	[ValueConversion(typeof(int), typeof(DrawingImage))]
	[ValueConversion(typeof(int), typeof(string))]
	class AvailabilityConverter 
		: IValueConverter
    {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int availability = (int)value;

			string imageName;
			string availabilityName;

			if (availability < 3000)
			{
				imageName = @"presenceUnknown";
				availabilityName = @"Unknown";
			}
			else if (availability < 4500)
			{
				imageName = @"presenceOnline";
				availabilityName = @"Available";
			}
			else if (availability < 6000)
			{
				imageName = @"presenceIdleOnline";
				availabilityName = @"Idle";
			}
			else if (availability < 7500)
			{
				imageName = @"presenceBusy";
				availabilityName = @"Busy";
			}
			else if (availability < 9000)
			{
				imageName = @"presenceIdleBusy";
				availabilityName = @"Busy (Away)";
			}
			else if (availability < 12000)
			{
				imageName = @"presenceDnd";
				availabilityName = @"Do Not Disturb";
			}
			else if (availability < 15000)
			{
				imageName = @"presenceAway";
				availabilityName = @"Be Right Back";
			}
			else if (availability < 18000)
			{
				imageName = @"presenceAway";
				availabilityName = @"Away";
			}
			else
			{
				imageName = @"presenceOffline";
				availabilityName = @"Offline";
			}

			if (targetType == typeof(ImageSource))
				return Application.Current.FindResource(imageName) as DrawingImage;

			return availabilityName;
		}

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

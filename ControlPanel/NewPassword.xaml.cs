using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ControlPanel
{
	/// <summary>
	/// Interaction logic for NewPassword.xaml
	/// </summary>
	public partial class NewPassword : Window
	{
		private DispatcherTimer timer;

		public NewPassword()
		{
			timer = new DispatcherTimer();
			timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
			timer.Tick += timer_Tick;
			timer.IsEnabled = true;

			InitializeComponent();
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			Indicator.Background = (Password.Password == Validation.Password) ? Brushes.Green : Brushes.Red;
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}

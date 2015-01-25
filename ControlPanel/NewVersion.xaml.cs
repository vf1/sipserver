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

namespace ControlPanel
{
	/// <summary>
	/// Interaction logic for NewVersion.xaml
	/// </summary>
	public partial class NewVersion : Window
	{
		public NewVersion()
		{
			InitializeComponent();

			DataContext = this;
		}

		public string Version { get; set; }
		public string Url { get; set; }

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}

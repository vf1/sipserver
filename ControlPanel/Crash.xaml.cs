using System;
using System.Collections.Generic;
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
	/// Interaction logic for Exception.xaml
	/// </summary>
	public partial class Crash : Window
	{
		public Crash()
		{
			InitializeComponent();
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			this.Report.SelectAll();
			this.Report.Copy();

			using (System.Diagnostics.Process process = new System.Diagnostics.Process())
			{
				process.StartInfo.FileName = "http://www.officesip.com/crash.html";
				process.StartInfo.UseShellExecute = true;
				process.Start();
			}

			Ok.IsEnabled = true;
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}

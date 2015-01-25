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
	/// Interaction logic for AuthTurnServer.xaml
	/// </summary>
	public partial class LoginTurnServer : Window
	{
		public LoginTurnServer()
		{
			InitializeComponent();
		}

		private void ok_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			this.Close();
		}

		private void address_TextChanged(object sender, TextChangedEventArgs e)
		{
			url.Text = string.IsNullOrEmpty(address.Text) ?
				"" : String.Format(@"net.tcp://{0}:10002/officesip.turn.server.n1", address.Text);
		}
	}
}

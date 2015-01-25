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
	/// Interaction logic for EditTurnServer.xaml
	/// </summary>
	public partial class EditTurnServer : Window
	{
		private OkEnabler okEnabler;

		public EditTurnServer()
		{
			this.okEnabler = new OkEnabler(this);

			InitializeComponent();
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void NewKeys_Click(object sender, RoutedEventArgs e)
		{
			byte[] key = new byte[20];

			Random random = new Random(Environment.TickCount);

			random.NextBytes(key);
			key1.Text = Convert.ToBase64String(key);

			random.NextBytes(key);
			key2.Text = Convert.ToBase64String(key);
		}
	}
}

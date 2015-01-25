using System;
using System.Net;
using System.Text;
using System.Windows;

namespace ControlPanel
{
	/// <summary>
	/// Interaction logic for AddVoipProvider.xaml
	/// </summary>
	public partial class AddVoipProvider : Window
	{
		private OkEnabler okEnabler;

		public AddVoipProvider()
		{
			this.okEnabler = new OkEnabler(this);

			InitializeComponent();
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}

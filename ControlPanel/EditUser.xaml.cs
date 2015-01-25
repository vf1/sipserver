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
	/// Interaction logic for EditUser.xaml
	/// </summary>
	public partial class EditUser : Window
	{
		private OkEnabler okEnabler;

		public EditUser()
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

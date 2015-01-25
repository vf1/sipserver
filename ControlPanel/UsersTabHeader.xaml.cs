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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ControlPanel
{
	/// <summary>
	/// Interaction logic for UsersTabHeader.xaml
	/// </summary>
	public partial class UsersTabHeader : UserControl
	{
		public UsersTabHeader(string text)
		{
			HeaderText = text;

			DataContext = this;

			InitializeComponent();
		}

		public string HeaderText
		{
			get;
			private set;
		}
	}
}

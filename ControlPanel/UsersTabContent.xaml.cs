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
	/// Interaction logic for UsersTabContent.xaml
	/// </summary>
	public partial class UsersTabContent : UserControl
	{
		public UsersTabContent(Service1.Users users)
		{
			Id = users.Id;

			InitializeComponent();
		}

		public string Id
		{
			get;
			private set;
		}
	}
}

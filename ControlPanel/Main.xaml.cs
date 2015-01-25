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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Main : Window
    {
        public Main()
        {
            InitializeComponent();
			CommandBindings.AddRange(Programme.Instance.CommandBindings);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Commands.Close.Execute(null, null);
		}

		public new void Close()
		{
			this.Closing -= Window_Closing;
			base.Close();
		}

		//private int processingIndicatorCount;

		//private void ShowProcessingIndicator()
		//{
		//    if (processingIndicatorCount++ == 0)
		//        processingIndicator.Visibility = Visibility.Visible;
		//}

		//private void HideProcessingIndicator()
		//{
		//    if (processingIndicatorCount > 0)
		//        if (--processingIndicatorCount == 0)
		//            processingIndicator.Visibility = Visibility.Hidden;
		//}

		//private void Button_Click(object sender, RoutedEventArgs e)
		//{
		//    ShowProcessingIndicator();
		//}

		//private void Button_Click_1(object sender, RoutedEventArgs e)
		//{
		//    HideProcessingIndicator();
		//}
	}
}

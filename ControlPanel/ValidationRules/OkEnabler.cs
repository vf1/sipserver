using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ControlPanel
{
	public class OkEnabler
	{
		private int errorCount;
		private Button ok;
		private Window window;

		public OkEnabler(Window window)
		{
			this.errorCount = 0;

			this.window = window;
			this.window.Loaded += Window_Loaded;
			this.window.Closed += Window_Closed;
			Validation.AddErrorHandler(this.window, new EventHandler<ValidationErrorEventArgs>(Validation_Error));
		}

		void Window_Closed(object sender, EventArgs e)
		{
			window.Loaded -= Window_Loaded;
			window.Closed -= Window_Closed;
			Validation.RemoveErrorHandler(window, new EventHandler<ValidationErrorEventArgs>(Validation_Error));
		}

		void Window_Loaded(object sender, RoutedEventArgs e)
		{
			DependencyObject element = LogicalTreeHelper.FindLogicalNode(this.window, @"ok");
			if (element != null && element is Button)
			{
				this.ok = element as Button;
				this.ok.IsEnabled = (errorCount == 0);
			}
		}

		private void Validation_Error(object sender, ValidationErrorEventArgs e)
		{
			if (e.Action == ValidationErrorEventAction.Added)
				errorCount++;
			if (e.Action == ValidationErrorEventAction.Removed)
				errorCount--;

			if (this.ok != null)
				this.ok.IsEnabled = (errorCount == 0);
		}
	}
}

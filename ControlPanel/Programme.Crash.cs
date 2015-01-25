using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Threading;

namespace ControlPanel
{
	partial class Programme
	{
		private string crashReport;

		void InitializeCrashHandler()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}

		void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception)
				crashReport = CreateCrashReport(e.ExceptionObject as Exception);
			else
				crashReport = CreateCrashReport(null);

			Thread newThread = new Thread(new ThreadStart(CrashThread));
			newThread.SetApartmentState(ApartmentState.STA);
			newThread.IsBackground = false;
			newThread.Start();
			newThread.Join();

			System.Environment.Exit(0);
		}

		private void CrashThread()
		{
			Crash crashWindow = new Crash();
			crashWindow.Report.Text = crashReport;
			crashWindow.Closed += new EventHandler(CrashWindow_Closed);
			crashWindow.Show();

			System.Windows.Threading.Dispatcher.Run();
		}

		private void CrashWindow_Closed(object sender, EventArgs e)
		{
			System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
		}

		private string CreateCrashReport(Exception ex)
		{
			string report = "";

			try
			{
				report += "Control Panel (version not specified)\r\n";

				report += System.Environment.OSVersion.ToString() + "\r\n";
				report += ".NET Framework " + System.Environment.Version.ToString() + "\r\n";
				report += "Environment.StackTrace: \r\n" + System.Environment.StackTrace + "\r\n";

				if (ex != null)
				{
					report += "Exception\r\n";

					report += "Message: " + ex.Message + "\r\n";
					report += "TargetSite: " + ex.TargetSite + "\r\n";
					report += "Source: " + ex.Source + "\r\n";
					report += "StackTrace: \r\n" + ex.StackTrace + "\r\n";
					report += "ToString: \r\n" + ex.ToString() + "\r\n";
				}
				else
				{
					report += "No Exception\r\n";
					report += "StackTrace: \r\n";

					StackTrace stackTrace = new StackTrace(true);

					for (int i = 0; i < stackTrace.FrameCount; i++)
					{
						StackFrame stackFrame = stackTrace.GetFrame(i);

						report += stackFrame.GetMethod() + "\r\n";
					}
				}

				report += "-- end of report --\r\n";
			}
			catch (Exception)
			{
			}

			return report;
		}
	}
}

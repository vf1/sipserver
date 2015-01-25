using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Net;
using Sip.Server.Configuration;

namespace Sip.Server
{
	partial class CrashHandler
	{
		private Uri uploadUri = new Uri("http://www.officesip.com/uprep.php");
		private string report;
		private Version version;

		public CrashHandler()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			report = CreateCrashReport(e.ExceptionObject as Exception, version);

			Tracer.WriteError(report);

#if DEBUG
			Tracer.WriteError(report);
#else
			Thread newThread = new Thread(new ThreadStart(SendReport));
			newThread.SetApartmentState(ApartmentState.STA);
			newThread.IsBackground = false;
			newThread.Start();
			newThread.Join();
#endif

			System.Environment.Exit(0);
		}

		private void SendReport()
		{
			try
			{
				var fileName = Path.GetTempFileName();
				File.WriteAllText(fileName, report, Encoding.Unicode);

				CredentialCache credentialCache = new CredentialCache();
				credentialCache.Add(uploadUri, @"Basic", new NetworkCredential(@"uprep", @"qeiusroi123woi3zf"));

				var webClient = new WebClient();
				webClient.Credentials = credentialCache.GetCredential(uploadUri, @"Basic");
				webClient.QueryString.Add("app", "SRV");
				webClient.QueryString.Add("ver", version.ToString());
				webClient.UploadFile(uploadUri, fileName);
			}
			catch
			{
			}
		}

		private static string CreateCrashReport(Exception ex, Version version)
		{
			string report = "";

			try
			{
				report += "OfficeSIP Server " + version.ToString() + "\r\n";

				report += System.Environment.OSVersion.ToString() + "\r\n";
				report += ".NET Framework " + System.Environment.Version.ToString() + "\r\n";

				if (ex != null)
				{
					report += "\r\n\r\n------------------------------------------[Exception\r\n";
					report += CreateExceptionReport(ex);
					for (var ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
					{
						report += "\r\n\r\n------------------------------------------[Inner Exception\r\n";
						report += CreateExceptionReport(ex2);
					}
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

				report += "\r\n------------------------------------------[Environment.StackTrace: \r\n" + System.Environment.StackTrace + "\r\n";

				report += "\r\n------------------------------------------[Application Settings: \r\n";
				try
				{
					report += "EXCLUDED!";
					//report += SipServerConfigurationSection.ReadRawXml();
				}
				catch
				{
					report += "Failed to report settings\r\n";
				}

				report += "\r\n-- end of report --\r\n";
			}
			catch
			{
			}

			return report;
		}

		private static string CreateExceptionReport(Exception ex)
		{
			return
				"Message: " + ex.Message + "\r\n" +
				"TargetSite: " + ex.TargetSite + "\r\n" +
				"Source: " + ex.Source + "\r\n" +
				"StackTrace: \r\n" + ex.StackTrace + "\r\n" +
				"\r\nToString: \r\n" + ex.ToString() + "\r\n";
		}
	}
}

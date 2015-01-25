using System;
using System.IO;
using System.Timers;
using System.Configuration;

namespace Server.Configuration
{
	class ConfigurationMonitor
		: IDisposable
	{
		private Timer timer;
		private FileSystemWatcher watcher;

		public event EventHandler<EventArgs> Changed;

		public void Dispose()
		{
			if (watcher != null)
			{
				watcher.EnableRaisingEvents = false;
				watcher.Dispose();
				watcher = null;
			}

			if (timer == null)
			{
				timer.Dispose();
				timer = null;
			}
		}

		public void StartMonitoring(ConfigurationSectionEx section)
		{
			if (timer == null)
			{
				timer = new Timer(500);
				timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
				timer.AutoReset = false;
			}

			if (watcher == null)
			{
				watcher = new FileSystemWatcher();
				watcher.Changed += Watcher_Changed;
				watcher.Created += Watcher_Changed;
				watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
			}

			watcher.EnableRaisingEvents = false;

			watcher.Path = Path.GetDirectoryName(section.FilePath);
            watcher.Filter = Path.GetFileName(section.FilePath);

			watcher.EnableRaisingEvents = true;
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			var changed = Changed;
			if (changed != null)
				changed(this, EventArgs.Empty);
		}

		private void Watcher_Changed(object sender, FileSystemEventArgs e)
		{
			if (timer.Enabled)
				timer.Stop();
			timer.Start();
		}
	}
}

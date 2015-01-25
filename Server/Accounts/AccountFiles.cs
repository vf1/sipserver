using System;
using System.IO;
using System.Collections.Generic;
using Sip.Tools;

namespace Sip.Server.Accounts
{
	class AccountFiles
		: AccountPath
		, IDisposable
	{
		private readonly MultiTimer<int> timer;
		private readonly FileSystemWatcher watcher;
		private readonly HashSet<int> ignoreWatcher;
		private readonly Action<int, string> readFile;
		private readonly HashSet<string> files;

		public AccountFiles(string fileName, Action<int, string> readFile)
			: base(fileName)
		{
			this.readFile = readFile;

			this.files = new HashSet<string>();

			this.timer = new MultiTimer<int>(Timer_Elapsed, 256, true, 500);

			this.ignoreWatcher = new HashSet<int>();

			this.watcher = new FileSystemWatcher(RootDirectory);
			this.watcher.Changed += Watcher_Changed;
			this.watcher.Created += Watcher_Changed;
			this.watcher.Deleted += Watcher_Changed;
			this.watcher.Renamed += Watcher_Renamed;
			this.watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.DirectoryName;
			this.watcher.IncludeSubdirectories = true;
			this.watcher.EnableRaisingEvents = true;
		}

		public void Dispose()
		{
			timer.Dispose();
			watcher.Dispose();
		}

		public void ReadAllFiles()
		{
			var items = Directory.GetFiles(RootDirectory, SearchPattern, SearchOption.AllDirectories);

			foreach (var item in items)
			{
				int? accountId = GetAccountId(item);

				if (accountId.HasValue)
				{
					files.Add(GetFileName(accountId.Value));
					readFile(accountId.Value, item);
				}
			}
		}

		public void IgnoryFileChanges(int accountId)
		{
			lock (ignoreWatcher)
				ignoreWatcher.Add(accountId);
		}

		private void Timer_Elapsed(int timerId, int accountId)
		{
			readFile(accountId, GetFileName(accountId));
		}

		private void Watcher_Renamed(object sender, RenamedEventArgs e)
		{
		}

		private void Watcher_Changed(object sender, FileSystemEventArgs e)
		{
			int? accountId = GetAccountId(e.FullPath);

			if (accountId.HasValue)
			{
				if (e.ChangeType == WatcherChangeTypes.Deleted)
					files.Remove(e.FullPath);
				else
					files.Add(e.FullPath);

				FileChanged(accountId.Value);
			}
			else
			{
				if (e.ChangeType == WatcherChangeTypes.Deleted)
				{
					var remove = new List<string>();

					foreach (var file in files)
						if (file.StartsWith(e.FullPath))
							remove.Add(file);

					foreach (var file in remove)
					{
						files.Remove(file);

						accountId = GetAccountId(e.FullPath);
						FileChanged(accountId.Value);
					}
				}
			}
		}

		private void FileChanged(int accountId)
		{
			bool removed;
			lock (ignoreWatcher)
				removed = ignoreWatcher.Remove(accountId);

			if (removed == false)
			{
				timer.RemoveByParam(accountId);
				timer.Add(accountId);
			}
		}
	}
}

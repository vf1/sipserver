using System;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using ThreadSafe = System.Collections.Generic.ThreadSafe;
using System.Globalization;
using System.Threading;
using Sip.Tools;
using Sip.Server.Accounts;
// http://www.codeproject.com/KB/database/CsvReader.aspx
// A Fast CSV Reader
// Sebastien Lorion | 6 Jul 2011
using LumenWorks.Framework.IO.Csv;

namespace Sip.Server.Users
{
	public class CsvUsers
		: BaseUsers
	{
		private const int maxSync = 32;

		private readonly ThreadSafe.Dictionary<int, Dictionary<string, CsvUser>> accounts;
		private readonly ReaderWriterLockSlim[] syncs;
		private readonly AccountFiles files;

		public CsvUsers(string fileName)
		{
			syncs = new ReaderWriterLockSlim[maxSync];
			for (int i = 0; i < syncs.Length; i++)
				syncs[i] = new ReaderWriterLockSlim();

			accounts = new ThreadSafe.Dictionary<int, Dictionary<string, CsvUser>>(1024);

			files = new AccountFiles(fileName, ReadUsers);
			files.ReadAllFiles();
		}

		public override void Dispose()
		{
			files.Dispose();
			accounts.Dispose();

			for (int i = 0; i < syncs.Length; i++)
				if (syncs[i] != null)
					syncs[i].Dispose();
		}

		public override string Id
		{
			get { return "csv"; }
		}

		public override string SourceName
		{
			get { return ".csv File"; }
		}

		public override bool HasPasswords
		{
			get { return true; }
		}

		public override bool IsReadOnly
		{
			get { return false; }
		}

		public override void Add(int accountId, IUser user)
		{
			if (string.IsNullOrEmpty(user.Name))
				throw new UsersException(ErrorCodes.UsernameEmpty);

			var sync = GetSync(accountId);
			sync.EnterWriteLock();
			try
			{
				var users = accounts.GetOrAdd(accountId, NewAccount);

				if (users.ContainsKey(user.Name))
					throw new UsersException(ErrorCodes.UserExist);

				users.Add(user.Name, new CsvUser(user));

				var fileName = files.GetFileName(accountId);

				if (Directory.Exists(Path.GetDirectoryName(fileName)) == false)
					Directory.CreateDirectory(Path.GetDirectoryName(fileName));

				File.AppendAllText(fileName, "\r\n" + CsvUser.ToString(user));
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public override void Update(int accountId, IUser user)
		{
			var sync = GetSync(accountId);
			sync.EnterWriteLock();
			try
			{
				var users = accounts.GetOrAdd(accountId, NewAccount);
				users[user.Name] = new CsvUser(user);

				WriteUsers(accountId, users);
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public override void Remove(int accountId, string username)
		{
			var sync = GetSync(accountId);
			sync.EnterWriteLock();
			try
			{
				var users = accounts.GetOrAdd(accountId, NewAccount);
				users.Remove(username);

				WriteUsers(accountId, users);
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public override int GetCount(int accountId)
		{
			var sync = GetSync(accountId);
			sync.EnterReadLock();
			try
			{
				var users = accounts.GetValue(accountId);
				return (users == null) ? 0 : users.Count;
			}
			finally
			{
				sync.ExitReadLock();
			}
		}

		public override IList<IUser> GetUsers(int accountId, int startIndex, int count)
		{
			var sync = GetSync(accountId);
			sync.EnterReadLock();
			try
			{
				return GetFromDictionary<CsvUser, IUser>(accounts.GetValue(accountId), startIndex, count);
			}
			finally
			{
				sync.ExitReadLock();
			}
		}

		public override IUser GetByName(int accountId, string username)
		{
			var sync = GetSync(accountId);
			sync.EnterReadLock();
			try
			{
				CsvUser user = null;

				var users = accounts.GetValue(accountId);
				if (users != null)
					users.TryGetValue(username, out user);

				return user;
			}
			finally
			{
				sync.ExitReadLock();
			}
		}

		private void ReadUsers(int accountId, string fileName)
		{
			var sync = GetSync(accountId);
			sync.EnterWriteLock();
			try
			{
				var users = NewAccount(accountId);
				accounts.Replace(accountId, users);

				using (var csv = new CsvReader(new StreamReader(fileName), false))
				{
					csv.SkipEmptyLines = true;
					csv.MissingFieldAction = MissingFieldAction.ReplaceByEmpty;
					csv.DefaultParseErrorAction = ParseErrorAction.AdvanceToNextLine;

					int fieldCount = csv.FieldCount;
					string[] headers = csv.GetFieldHeaders();

					while (csv.ReadNextRecord())
					{
						var user = new CsvUser();

						if (user.Read(csv))
							if (users.ContainsKey(user.Name) == false)
								users.Add(user.Name, user);
					}
				}
			}
			catch (Exception)
			{
				// должно вызываться снаружи лока
				//OnError(ex.Message);
			}
			finally
			{
				sync.ExitWriteLock();
			}

			OnReset(accountId);
		}

		private void WriteUsers(int accountId, Dictionary<string, CsvUser> users)
		{
			files.IgnoryFileChanges(accountId);
			using (var file = File.CreateText(files.GetFileName(accountId)))
			{
				files.IgnoryFileChanges(accountId);
				file.WriteLine("# " + CsvUser.GetFormatDescription());

				foreach (var user in users)
				{
					files.IgnoryFileChanges(accountId);
					file.WriteLine(user.Value.ToString());
				}
			}
		}

		private Func<int, Dictionary<string, CsvUser>> NewAccount =
			(accountId) =>
			{
				return new Dictionary<string, CsvUser>();
			};

		private ReaderWriterLockSlim GetSync(int accountId)
		{
			return syncs[accountId % maxSync];
		}
	}
}

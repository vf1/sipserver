using System;
using System.Collections.Generic;
using ThreadSafe = System.Collections.Generic.ThreadSafe;
using System.Threading;
using Base.Message;

namespace Sip.Server.Accounts
{
	public class Accountx
		: IDisposable
		, IAccounts
	{
		private readonly AccountFiles files;
		private readonly ThreadSafe.Dictionary<int, IAccount> accounts1;
		private readonly ThreadSafe.Dictionary<string, IAccount> accounts2;

		private int count;

		public Accountx(string fileName)
		{
			this.accounts1 = new ThreadSafe.Dictionary<int, IAccount>();
			this.accounts2 = new ThreadSafe.Dictionary<string, IAccount>();

			this.files = new AccountFiles(fileName, ReadAccount);
			this.files.ReadAllFiles();
		}

		public void Dispose()
		{
			files.Dispose();
			accounts1.Dispose();
			accounts2.Dispose();
		}

		public IList<IAccount> GetAccounts()
		{
			return accounts1.ToList();
		}

		public IAccount GetAccount(int id)
		{
			return accounts1.GetValue(id);
		}

		public IAccount GetAccount(ByteArrayPart domain)
		{
			return domain.IsInvalid ? null : accounts2.GetValue(domain.ToString());
		}

		public bool HasDomain(ByteArrayPart domain)
		{
			return domain.IsValid && accounts2.ContainsKey(domain.ToString());
		}

		public void ForEach(Action<IAccount> action)
		{
			accounts1.ForEach(action);
		}

		#region FOR DEPRECATED WCF

		private IAccount defaultAccount = null;

		public IAccount GetDefaultAccount()
		{
			if (defaultAccount == null || accounts1.ContainsKey(defaultAccount.Id) == false)
			{
				defaultAccount = accounts1.First((a) => true);

				if (defaultAccount != null)
				{
					accounts1.ForEach(
						(other) =>
						{
							if (other.Id < defaultAccount.Id)
								defaultAccount = other;
						});
				}
				else
				{
					int id = AddAccount(new Account()
					{
						DomainName = @"officesip.local",
					});

					defaultAccount = accounts1.GetValue(id);
				}
			}

			return defaultAccount;
		}

		public int DefaultAccountId
		{
			get
			{
				var account = GetDefaultAccount();

				return (account == null) ? -1 : account.Id;
			}
		}

		#endregion

		public void Remove(Base.Message.ByteArrayPart part)
		{
			string domainName = part.ToString();

			var account = accounts2.GetValue(domainName);
			if (account != null)
			{
				accounts1.Remove(account.Id);
				accounts2.Remove(account.DomainName);

				Account.Delete(files.GetFileName(account.Id));

				try
				{
					var directory = files.GetAccountRootDirectory(account.Id);
					if (directory != null)
						System.IO.Directory.Delete(directory, true);
				}
				catch
				{
				}
			}
		}

		public int SetAccount(IAccount account)
		{
			if (account.Id == Account.InvalidId)
			{
				return AddAccount(account);
			}
			else
			{
				UpdateAccount(account);
				return account.Id;
			}
		}

		public void UpdateAccount(IAccount account)
		{
			if (account.Id == Account.InvalidId)
				throw new AccountsException(AccountsErrors.InvalidId);

			var newOne = new Account(account);
			var oldOne = accounts1.GetValue(account.Id);

			if (oldOne == null)
				throw new AccountsException(AccountsErrors.OldNotFound);
			if (string.IsNullOrEmpty(newOne.DomainName))
				throw new AccountsException(AccountsErrors.DomainRequred);

			if (newOne.DomainName != oldOne.DomainName)
			{
				if (accounts2.TryAdd(newOne.DomainName, newOne) == false)
					throw new AccountsException(AccountsErrors.DomainUsed);
				accounts2.Remove(oldOne.DomainName);
			}

			files.IgnoryFileChanges(account.Id);
			newOne.Serialize(files.GetFileName(account.Id));
		}

		public int AddAccount(IAccount account)
		{
			int id = Interlocked.Increment(ref count);
			var newOne = new Account(id, account);

			if (string.IsNullOrEmpty(newOne.DomainName))
				throw new AccountsException(AccountsErrors.DomainRequred);

			if (accounts2.TryAdd(newOne.DomainName, newOne) == false)
				throw new AccountsException(AccountsErrors.DomainUsed);
			if (accounts1.TryAdd(id, newOne) == false)
				throw new AccountsException(AccountsErrors.IdUsed);

			try
			{
				files.IgnoryFileChanges(id);
				newOne.Serialize(files.GetFileName(id));
			}
			catch
			{
				accounts2.Remove(newOne.DomainName);
				accounts1.Remove(id);
				throw;
			}

			return id;
		}

		private void ReadAccount(int id, string fileName)
		{
			IAccount account;

			try
			{
				account = Account.Deserialize(id, fileName);
			}
			catch (Exception)
			{
				account = null;
			}

			if (account == null)
			{
				account = accounts1.GetValue(id);

				if (account != null)
				{
					accounts1.Remove(id);
					accounts2.Remove(account.DomainName);
				}
			}
			else
			{
				accounts1.Replace(id, account);
				accounts2.Replace(account.DomainName, account);
			}

			for (; ; )
			{
				int count1 = Thread.VolatileRead(ref count);
				if (count1 >= id)
					break;
				if (Interlocked.CompareExchange(ref count, id, count1) == count1)
					break;
			}
		}
	}
}

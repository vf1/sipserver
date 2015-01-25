using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Threading;
using System.Linq;

using Sip.Server.Tools;

namespace Sip.Server.Users
{
	public sealed class AdUsers
		: BaseUsers
	{
		public override string Id
		{
			get { return "ad"; }
		}

		public override string SourceName
		{
			get { return "Active Directory"; }
		}

		public override bool HasPasswords
		{
			get { return true; }
		}

		public override bool IsReadOnly
		{
			get { return true; }
		}

		public override void Add(int accountId, IUser user)
		{
			throw new NotSupportedException();
		}

		public override void Update(int accountId, IUser user)
		{
			throw new NotSupportedException();
		}

		public override void Remove(int accountId, string name)
		{
			throw new NotSupportedException();
		}

		public override int GetCount(int accountId)
		{
			return users.Count;
		}

		public override IList<IUser> GetUsers(int accountId, int startIndex, int count)
		{
			lock (ProcessingLock)
			{
				//sync.EnterReadLock();
				try
				{
					return GetFromDictionary<AdUser, IUser>(users, startIndex, count);
				}
				finally
				{
					//sync.ExitReadLock();
				}
			}
		}

		public override IUser GetByName(int accountId, string name)
		{
			lock (ProcessingLock)
			{
				AdUser user;
				users.TryGetValue(name, out user);

				return user;
			}
		}

		private readonly object ProcessingLock = new object();

		private string group;
		//private readonly string domainName;
		private readonly Dictionary<string, AdUser> users;

		private const string WKO_GUID_DELETED_OBJECTS_CONTAINER = @"18e2ea80684f11d2b9aa00c04f79f805";
		private const string LDAP_MATCHING_RULE_BIT_AND = @"1.2.840.113556.1.4.803";
		private const string LDAP_MATCHING_RULE_BIT_OR = @"1.2.840.113556.1.4.804";

		private const int UpdatePeriodFail = 30 * 1000;
		private const int UpdatePeriod = 3 * 60 * 1000;

		private bool m_StartUpdate;
		private Timer m_UpdateTimer;
		private DirectoryEntry m_rootDSE;
		private DirectoryEntry m_NTDSSettings;
		/// <summary>
		/// rootDSE.highestCommittedUSN.
		/// </summary>
		private long m_highestCommittedUSN;
		/// <summary>
		/// rootDSE.dnsHostName.
		/// </summary>
		private string m_dnsHostName;
		/// <summary>
		/// rootDSE.defaultNamingContext.
		/// </summary>
		private string m_defaultNamingContext;
		/// <summary>
		/// rootDSE.dsServiceName.
		/// </summary>
		private string m_dsServiceName;
		/// <summary>
		/// NTDS.invocationID.
		/// </summary>
		private Guid m_invocationID;
		/// <summary>
		/// Следующий USN для которого необходимо выполнить синхронизацию с AD.
		/// </summary>
		private long m_NextUSN;
		/// <summary>
		/// distinguishedName группы пользователей.
		/// </summary>
		private string m_distinguishedNameGroup;

		//string domainName, 
		public AdUsers(string group)
		{
			users = new Dictionary<string, AdUser>();
			//Global.evParameterChanged += ConfigurationChanged;
			m_UpdateTimer = new Timer(new TimerCallback(UpdateTimerProcessing), null, 1, Timeout.Infinite);
			//this.domainName = domainName;
			this.group = group;
		}

		public override void Dispose()
		{
			m_UpdateTimer.Dispose();
			//Global.evParameterChanged -= ConfigurationChanged;
		}

		public string Group
		{
			get { return group; }
			set
			{
				if (value != group)
				{
					group = value;
					UpdateTimerProcessing(null);
				}
			}
		}

		private void ADUpdateInfo()
		{
			if (m_rootDSE == null)
				m_rootDSE = new DirectoryEntry(@"LDAP://rootDSE");
			m_rootDSE.RefreshCache();

			var dnsHostName = m_rootDSE.Properties[@"dnsHostName"].Value as string;
			if (m_dnsHostName != dnsHostName)
			{
				m_dnsHostName = dnsHostName;
				m_StartUpdate = true;
			}

			var defaultNamingContext = m_rootDSE.Properties[@"defaultNamingContext"].Value as string;
			if (m_defaultNamingContext != defaultNamingContext)
			{
				m_defaultNamingContext = defaultNamingContext;
				m_StartUpdate = true;
			}

			var dsServiceName = m_rootDSE.Properties[@"dsServiceName"].Value as string;
			if (m_dsServiceName != dsServiceName)
			{
				m_dsServiceName = dsServiceName;
				m_StartUpdate = true;
			}

			if (m_highestCommittedUSN == 0)
				if (m_NTDSSettings != null)
				{
					m_NTDSSettings.Dispose();
					m_NTDSSettings = null;
				}

			if (m_NTDSSettings == null)
				m_NTDSSettings = new DirectoryEntry(@"LDAP://" + m_dnsHostName + @"/" + m_dsServiceName);
			m_NTDSSettings.RefreshCache();

			var invocationID = new Guid(m_NTDSSettings.Properties[@"invocationID"].Value as byte[]);
			if (invocationID.CompareTo(m_invocationID) != 0)
			{
				m_invocationID = invocationID;
				m_StartUpdate = true;
			}

			var distinguishedNameGroup = String.Empty;
			if (group != String.Empty)
			{
				var result = ADSearch(
					@"LDAP://" + m_defaultNamingContext,
					@"(&(objectCategory=group)(sAMAccountName=" + group + @"))",
					new string[]
					{
						@"distinguishedName"
					},
					false);

				distinguishedNameGroup = result.Count == 1 ? result[0].Property(@"distinguishedName") : null;

				result.Dispose();
			}

			if (m_distinguishedNameGroup != distinguishedNameGroup)
			{
				m_distinguishedNameGroup = distinguishedNameGroup;
				m_StartUpdate = true;
			}

			m_highestCommittedUSN = Convert.ToInt64(m_rootDSE.Properties[@"highestCommittedUSN"].Value as string);

			if (m_StartUpdate == true)
				Tracer.WriteInformation("Updated Active Directory info. \r\n\r\n" +
					@"         dnsHostName: " + m_dnsHostName + "\r\n" +
					@"defaultNamingContext: " + m_defaultNamingContext + "\r\n" +
					@"       dsServiceName: " + m_dsServiceName + "\r\n" +
					@"        invocationID: " + m_invocationID + "\r\n" +
					@"         Users Group: " + (m_distinguishedNameGroup == null ? String.Empty : m_distinguishedNameGroup) + "\r\n");
		}

		private string GetWellKnownObjectsPath(string guid)
		{
			return @"LDAP://<WKGUID=" + guid + @"," + m_defaultNamingContext + @">";
		}

		/// <summary>
		/// Поиск в AD с заданными параметрами поиска.
		/// </summary>
		private SearchResultCollection ADSearch(string path, string filter, string[] propertiesToLoad, bool tombstone)
		{
			return new DirectorySearcher(
				new DirectoryEntry(path, null, null, AuthenticationTypes.Secure | AuthenticationTypes.FastBind),
				filter,
				propertiesToLoad,
				SearchScope.Subtree)
			{
				CacheResults = false,
				ClientTimeout = new TimeSpan(0, 0, 120),
				PageSize = 256,
				ServerPageTimeLimit = new TimeSpan(0, 0, 60),
				ServerTimeLimit = new TimeSpan(0, 0, 120),
				Tombstone = tombstone
			}.FindAll();
		}

		/// <summary>
		/// Синхронизация данных пользователей AD.
		/// </summary>
		private void ADSynchUsers()
		{
			if (m_distinguishedNameGroup == null)
			{
				users.Clear();
				OnReset(-1);
			}
			else
			{
				m_NextUSN = 0; // vf: временное решение, не обновляется при удалении/добавлении пользователей из/в группу без этого

				var remove = new List<string>();

				// обработка недействительных пользователей
				// заблокированные или отключенные пользователи, пользователи вне группы
				using (var result = ADSearch(
					@"LDAP://" + m_defaultNamingContext,
					@"(&(objectCategory=person)(|(userAccountControl:" + LDAP_MATCHING_RULE_BIT_OR + @":=18)" + (m_distinguishedNameGroup != String.Empty ? @"(!memberOf=" + m_distinguishedNameGroup + @")" : @"") + @")(uSNChanged>=" + m_NextUSN.ToString() + @"))",
					new string[] { @"sAMAccountName", },
					false))
				{
					remove.AddRange(result.Property(@"sAMAccountName"));
				}

				// удаленные пользователи
				using (var result = ADSearch(
					GetWellKnownObjectsPath(WKO_GUID_DELETED_OBJECTS_CONTAINER),
					@"(&(objectClass=user)(isDeleted=TRUE)(uSNChanged>=" + m_NextUSN.ToString() + @"))",
					new string[] { @"sAMAccountName" },
					true))
				{
					remove.AddRange(result.Property(@"sAMAccountName"));
				}

				remove.ForEach(i =>
				{
					AdUser removed;
					users.TryGetValue(i.ToLower(), out removed);
					if (users.Remove(i.ToLower()) == true)
						OnRemoved(-1, removed);
				});

				// обработка действительных пользователей
				using (var result = ADSearch(
					@"LDAP://" + m_defaultNamingContext,
					@"(&(objectCategory=person)(!userAccountControl:" + LDAP_MATCHING_RULE_BIT_OR + @":=18)" + (m_distinguishedNameGroup != String.Empty ? @"(memberOf=" + m_distinguishedNameGroup + @")" : @"") + @"(uSNChanged>=" + m_NextUSN.ToString() + @"))",
					AdUser.Properties,
					false))
				{
					foreach (SearchResult searchResult in result)
					{
						AdUser oldUser, newUser = new AdUser(searchResult);

						if (users.TryGetValue(newUser.Name, out oldUser) == false)
						{
							users.Add(newUser.Name, newUser);
							OnAdded(-1, newUser);
						}
						else
						{
							if (oldUser.Equals(newUser))
							{
								oldUser.CopyFrom(newUser);
								OnUpdated(-1, newUser);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Обработчик таймера обновления.
		/// </summary>
		private void UpdateTimerProcessing(Object state)
		{
			lock (ProcessingLock)
			{
				bool update = false;

				try
				{
					ADUpdateInfo();

					if (m_StartUpdate == true)
					{
						m_StartUpdate = false;
						m_NextUSN = 0;
						update = true;
					}
					else
						if (m_highestCommittedUSN >= m_NextUSN)
							update = true;

					if (update == true)
					{
						ADSynchUsers();
						m_NextUSN = m_highestCommittedUSN + 1;

						Tracer.WriteInformation("Update AD users: " + users.Count);
					}

					m_UpdateTimer.Change(UpdatePeriod, Timeout.Infinite);
				}
				catch (ObjectDisposedException)
				{
				}
				catch (Exception ex)
				{
					Tracer.WriteException("AD users", ex);

					m_UpdateTimer.Change(UpdatePeriodFail, Timeout.Infinite);
				}
			}
		}
	}

	static class Extensions
	{
		static public string Property(this SearchResult result, string name)
		{
			var s = String.Empty;

			try
			{
				s = result.Properties[name][0] as string;
			}
			catch
			{
			}

			return s;
		}

		static public List<string> Property(this SearchResultCollection result, string name)
		{
			var list = new List<string>();

			foreach (SearchResult i in result)
			{
				var s = i.Property(name);

				if (list.Contains(s) == false)
					list.Add(s);
			}

			return list;
		}
	}
}

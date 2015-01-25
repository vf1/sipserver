using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Sip.Server.Users
{
	public class Userz
		: IDisposable
		, IUserz
	{
		private string customUsersPath;
		private IUsers[] userz;

		public Userz(string customUsersPath)
		{
			this.userz = new IUsers[0];
			this.customUsersPath = customUsersPath;
		}

		public void Dispose()
		{
			foreach (var users in userz)
				users.Dispose();
		}

		#region Events: Reset, Added, Updated, Removed

		public event IUsersEventHandler2 Reset
		{
			add
			{
				foreach (var users in userz)
					users.Reset += value;
			}
			remove
			{
				foreach (var users in userz)
					users.Reset -= value;
			}
		}

		public event IUsersEventHandler1 Added
		{
			add
			{
				foreach (var users in userz)
					users.Added += value;
			}
			remove
			{
				foreach (var users in userz)
					users.Added -= value;
			}
		}

		public event IUsersEventHandler1 Updated
		{
			add
			{
				foreach (var users in userz)
					users.Updated += value;
			}
			remove
			{
				foreach (var users in userz)
					users.Updated -= value;
			}
		}

		public event IUsersEventHandler1 Removed
		{
			add
			{
				foreach (var users in userz)
					users.Removed += value;
			}
			remove
			{
				foreach (var users in userz)
					users.Removed -= value;
			}
		}

		#endregion

		//public IUsers[] GetHasPasswordUsers()
		//{
		//    var result = new List<IUsers>();

		//    foreach (var users in userz)
		//        if (users.HasPasswords)
		//            result.Add(users);

		//    return result.ToArray();
		//}

		//public IUsers[] GetAllUsers()
		//{
		//    return userz;
		//}

		public int GetIndex(string id)
		{
			for (int i = 0; i < userz.Length; i++)
			{
				if (userz[i].Id == id)
					return i;
			}

			return -1;
		}

		public IUsers Get(string id)
		{
			int index = GetIndex(id);

			return (index < 0) ? null : userz[index];
		}

		public IUsers this[int index]
		{
			get { return userz[index]; }
		}

		public int Count
		{
			get { return userz.Length; }
		}

		public IUsers[] ToArray()
		{
			return userz;
		}

		public void Add(IUsers users)
		{
			if (GetIndex(users.Id) >= 0)
				throw new ArgumentException(@"IUsers with Id " + users.Id + " already exist in userz");

			Array.Resize<IUsers>(ref userz, userz.Length + 1);
			userz[userz.Length - 1] = users;
		}

		public void LoadCustomUsers()
		{
			try
			{
				foreach (var dll in Directory.GetFiles(customUsersPath, @"*.dll"))
				{
					try
					{
						var obj = Activator.CreateInstanceFrom(dll, Path.GetFileNameWithoutExtension(dll) + ".Users");

						Add((IUsers)obj.Unwrap());
					}
					catch (Exception ex)
					{
						Tracer.WriteException("Load Custom Users", ex);
					}
				}
			}
			catch (Exception)
			{
			}
		}
	}
}

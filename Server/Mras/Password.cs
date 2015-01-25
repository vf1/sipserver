using System;
using System.Security.Cryptography;

namespace Mras
{
	public class Password
	{
		public static byte[] GetBytes(byte[] key2, byte[] username)
		{
			using (HMACSHA1 sha1 = new HMACSHA1(key2))
			{
				sha1.ComputeHash(username);
				return sha1.Hash;
			}
		}
	}
}

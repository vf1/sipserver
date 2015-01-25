using System;
using System.Security.Cryptography;

namespace Mras
{
	public class Username
	{
		public static byte[] GetBytes(byte[] key1, TokenBlob1 blob1)
		{
			using (HMACSHA1 sha1 = new HMACSHA1(key1))
			{
				byte[] username = blob1.GetBytes(sha1.HashSize / 8);

				sha1.ComputeHash(username, 0, blob1.Size);

				Array.Copy(sha1.Hash, 0, username, blob1.Size, sha1.HashSize / 8);

				return username;
			}
		}
	}
}

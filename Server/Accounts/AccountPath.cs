using System;
using System.IO;
using System.Globalization;

namespace Sip.Server.Accounts
{
	class AccountPath
	{
		public const string AccountTag = @"{account-id}";

		private readonly string baseFileName;
		private readonly int expectedLength;

		public AccountPath(string baseFileName)
		{
			this.baseFileName = baseFileName;
			this.expectedLength = GetFileName(0).Length;
		}

		public string RootDirectory
		{
			get
			{
				int index = baseFileName.IndexOf(AccountTag);
				if (index >= 0)
					return baseFileName.Substring(0, index);

				return Path.GetDirectoryName(baseFileName);
			}
		}

		public string GetAccountRootDirectory(int accountId)
		{
			int index = baseFileName.IndexOf(AccountTag);

			if (index >= 0)
				return baseFileName
					.Substring(0, index + AccountTag.Length)
					.Replace(AccountTag, accountId.ToString("x8"));

			return null;
		}

		public string SearchPattern
		{
			get
			{
				return Path.GetFileName(baseFileName);
			}
		}

		public string GetFileName(int accountId)
		{
			return baseFileName.Replace(AccountTag, accountId.ToString("x8"));
		}

		public int? GetAccountId(string fileName)
		{
			int start = baseFileName.IndexOf(AccountTag);

			if (start > 0 && expectedLength == fileName.Length)
			{
				int result = -1;
				if (int.TryParse(fileName.Substring(start, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
					if (GetFileName(result) == fileName)
						return result;
			}

			return null;
		}
	}
}

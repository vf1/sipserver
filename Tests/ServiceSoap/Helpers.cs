using System;

namespace ServiceSoap
{
	static class Helpers
	{
		public static int GetEqualLength(this string s1, string s2)
		{
			int i;
			for (i = 0; i < s1.Length && i < s2.Length; i++)
			{
				if (s1[i] != s2[i])
					break;
			}
			return i;
		}
	}
}

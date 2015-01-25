using System;

namespace Sip.Sdp
{
	class Helpers
	{
		public static ArraySegment<byte> CutCandidates(ArraySegment<byte> content)
		{
			var str = System.Text.Encoding.UTF8.GetString(content.Array, content.Offset, content.Count);

			for (; ; )
			{
				int begin = str.IndexOf("a=candidate:");
				if (begin < 0)
					break;
				int end = str.IndexOf("\r\n", begin);
				if (end < 0)
					break;

				str = str.Remove(begin, end - begin + 2);
			}

			return new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(str));
		}
	}
}

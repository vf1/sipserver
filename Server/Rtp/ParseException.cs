using System;

namespace Rtp
{
	class ParseException
		: Exception
	{
		public ParseException(string message)
			: base(message)
		{
		}
	}
}

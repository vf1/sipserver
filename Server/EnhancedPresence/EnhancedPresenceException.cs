using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnhancedPresence
{
	public class EnhancedPresenceException :
		Exception
	{
		public EnhancedPresenceException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}

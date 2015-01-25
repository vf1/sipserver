using System;

namespace Sip.Simple
{
	struct Subscription
		: IEquatable<Subscription>
	{
		private int expiresTickCount;

		public Subscription(string id, int expires)
		{
			Id = id;
			expiresTickCount = unchecked(Environment.TickCount + expires * 1000);
		}

		public readonly string Id;

		public int Expires
		{
			get
			{
				int expires = (expiresTickCount - Environment.TickCount) / 1000;
				return (expires >= 0) ? expires : 0;
			}
		}

		public bool Equals(Subscription other)
		{
			return Id.Equals(other.Id);
		}
	}
}

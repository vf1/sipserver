using System;
using System.Threading;
using System.Collections.Generic;
using ThreadSafe = System.Collections.Generic.ThreadSafe;

namespace Sip.Simple
{
	sealed class Publication
	{
		private readonly ThreadSafe.List<Subscription> subscriptions;
		private readonly PresenceDocument document;

		public Publication(string aor)
		{
			document = new PresenceDocument(aor);
			subscriptions = new ThreadSafe.List<Subscription>(
				new List<Subscription>());
		}

		public ThreadSafe.List<Subscription> Subscriptions
		{
			get { return subscriptions; }
		}

		public PresenceDocument Document
		{
			get { return document; }
		}
	}
}

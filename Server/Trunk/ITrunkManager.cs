using System;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	interface ITrunkManager
	{
		event Action<Trunk> TrunkAdded;
		event Action<Trunk> TrunkRemoved;

		//Trunk GetTrunk(ByteArrayPart host);
		Trunk GetTrunkByDomain(ByteArrayPart host);
		Trunk GetTrunkById(int id);
	}
}

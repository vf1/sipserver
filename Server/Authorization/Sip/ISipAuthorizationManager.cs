using System;
using Sip.Message;
using Server.Authorization;

namespace Server.Authorization.Sip
{
	interface ISipAuthorizationManager
		: IAuthorizationManager<SipMessageReader, SipMessageWriter>
	{
		void WriteSignature(SipMessageWriter writer);
	}
}

using System;
using System.Collections.Generic;
using Base.Message;
using Sip.Message;
using Server.Authorization;

namespace Server.Authorization.Sip
{
	class SipAuthorizationManager
		: AuthorizationManager<SipMessageReader, SipMessageWriter, AuthSchemes>
		, ISipAuthorizationManager
	{
		protected override bool ValidateAuthorizationInternal(SipMessageReader reader, ByteArrayPart username, int param)
		{
			return true;
		}

		public void WriteSignature(SipMessageWriter writer)
		{
			SipMicrosoftAuthentication.SignMessage(writer);
		}

		protected override SipMessageWriter GetResponseBegin(SipMessageReader reader)
		{
			var writer = new SipResponseWriter();
			writer.WriteStatusLine(StatusCodes.Unauthorized);
			writer.CopyViaToFromCallIdRecordRouteCSeq(reader, StatusCodes.Unauthorized);

			return writer;
		}

		protected override void WriteMessageEnd(SipMessageWriter writer)
		{
			writer.WriteContentLength(0);
			writer.WriteCRLF();
		}

		protected override bool CanTryAgainWhenFail(AuthSchemes scheme)
		{
			return scheme == AuthSchemes.Digest;
		}
	}
}

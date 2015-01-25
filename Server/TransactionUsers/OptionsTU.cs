using System;
using System.Collections.Generic;
using Sip;
using Sip.Simple;
using Sip.Message;
using SocketServers;

namespace Sip.Server
{
	class OptionsTU
		: BaseTransactionUser
	{
		public static readonly Methods[] AllowMethods = new Methods[] { Methods.Registerm, Methods.Invitem, 
			Methods.Ackm, Methods.Cancelm, Methods.Byem, Methods.Optionsm, Methods.Subscribem,
			Methods.Notifym, Methods.Publishm, Methods.Servicem, };


		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			yield return new AcceptedRequest(this)
			{
				Method = Methods.Optionsm,
				IsAcceptedRequest = (reader) => true,
				AuthorizationMode = AuthorizationMode.Disabled,
				IncomingRequest = ProccessOptions,
			};
		}

		private void ProccessOptions(AcceptedRequest tu, IncomingMessageEx request)
		{
			var writer = GetWriter();

			writer.WriteStatusLine(StatusCodes.OK);
			writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, StatusCodes.OK); // statusCode есть во writer'е!!!!!!
			writer.WriteAllow(AllowMethods);
			writer.WriteCRLF();

			tu.SendResponse(request, writer);
		}
	}
}

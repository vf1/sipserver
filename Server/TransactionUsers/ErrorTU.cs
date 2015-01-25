using System;
using System.Collections.Generic;
using Sip.Message;

namespace Sip.Server
{
	class ErrorTU
		: BaseTransactionUser
	{
		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			foreach (Methods method in Enum.GetValues(typeof(Methods)))
				if (method != Methods.None)
					yield return new AcceptedRequest(this)
					{
						Method = method,
						IsAcceptedRequest = (reader) => true,
						IncomingRequest = ProccessRequest,
						AuthorizationMode = AuthorizationMode.Disabled,
					};
		}

		private void ProccessRequest(AcceptedRequest tu, IncomingMessageEx request)
		{
			var writer = GetWriter();

			writer.WriteStatusLine(StatusCodes.NotImplemented);
			writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, StatusCodes.NotImplemented);
			writer.WriteContentLength(0);
			writer.WriteCRLF();

			tu.SendResponse(request, writer);
		}
	}
}

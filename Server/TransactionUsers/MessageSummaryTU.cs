using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sip;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	class MessageSummaryTU
		: BaseTransactionUser
	{
		private static readonly ByteArrayPart contentType = new ByteArrayPart(@"application/simple-message-summary");
		private static readonly byte[] content = Encoding.UTF8.GetBytes("Messages-Waiting: no\r\n");
		//private static readonly byte[] subscribeType = Encoding.UTF8.GetBytes(@"application");
		//private static readonly byte[] subscribeSubtype = Encoding.UTF8.GetBytes(@"simple-message-summary");

		private readonly ProducedRequest notifyProducer;
		private readonly DialogManager dialogManager;

		public MessageSummaryTU()
		{
			dialogManager = new DialogManager();
			notifyProducer = new ProducedRequest(this);
		}

		public void Dispose()
		{
		}

		public override IEnumerable<ProducedRequest> GetProducedRequests()
		{
			yield return notifyProducer;
		}

		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			yield return new AcceptedRequest(this)
			{
				Method = Methods.Subscribem,
				AuthorizationMode = AuthorizationMode.Disabled,
				IsAcceptedRequest = (reader) =>
				{
					int accepted = reader.FindHeaderIndex(HeaderNames.Accept, 0);

					if (accepted < 0)
						return false;

					var result = reader.Headers[accepted].Value.EndWith(contentType) && AcceptedRequest.IsToEqualsFrom(reader);

					return result;
				},
				IncomingRequest = ProccessSubscribe,
			};
		}

		private void ProccessSubscribe(AcceptedRequest tu, IncomingMessageEx request)
		{
			var statusCode = StatusCodes.OK;
			var dialog = dialogManager.GetOrCreate(request.Reader, request.ConnectionAddresses, out statusCode);

			var writer = GetWriter();

			int expires = 0;
			if (statusCode == StatusCodes.OK)
			{
				expires = request.Reader.GetExpires(600, int.MaxValue);

				writer.WriteStatusLine(statusCode);
				writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, statusCode, dialog.LocalTag);
				writer.WriteDate(DateTime.UtcNow);
				writer.WriteExpires(expires);
				writer.WriteContentLength(0);
				writer.WriteCRLF();
			}
			else
			{
				writer.WriteResponse(request.Reader, statusCode);
			}

			tu.SendResponse(request, writer);

			if (statusCode == StatusCodes.OK)
				SendNotify(dialog, expires);
		}

		private void SendNotify(Dialog dialog, int expires)
		{
			int transactionId = GetTransactionId(Methods.Notifym);

			var writer = GetWriter();

			writer.WriteRequestLine(Methods.Notifym, dialog.RemoteUri);
			writer.WriteVia(dialog.Transport, dialog.LocalEndPoint, transactionId);
			writer.WriteFrom(dialog.LocalUri, dialog.LocalTag);
			writer.WriteTo(dialog.RemoteUri, dialog.RemoteTag);
			writer.WriteDate(DateTime.UtcNow);
			writer.WriteCallId(dialog.CallId);
			writer.WriteCseq(dialog.GetNextLocalCseq(), Methods.Notifym);
			writer.WriteContact(dialog.LocalEndPoint, dialog.Transport);
			//Event: message-summary
			//writer.WriteEvent();
			writer.WriteSubscriptionState(expires);
			writer.WriteMaxForwards(70);
			writer.WriteContentType(contentType);
			writer.WriteContentLength(content.Length);
			writer.WriteCRLF();

			writer.Write(content);

			notifyProducer.SendRequest(dialog.ConnectionAddresses, writer, transactionId);
		}
	}
}

using System;
using System.Threading;
using System.Collections.Generic;
using Sip;
using Sip.Simple;
using Sip.Message;
using Base.Message;
using SocketServers;

namespace Sip.Server
{
	class SimpleTU
		: BaseTransactionUser
	{
		private static readonly ByteArrayPart presence = new ByteArrayPart(@"presence");
		private static readonly ByteArrayPart pidfXml = new ByteArrayPart(@"pidf+xml");
		private static readonly ByteArrayPart application = new ByteArrayPart(@"application");

		private readonly SimpleModule simpleModule;
		private readonly DialogManager dialogManager;
		private readonly ProducedRequest notifyProducer;


		public SimpleTU(SimpleModule simpleModule)
		{
			this.dialogManager = new DialogManager();

			this.simpleModule = simpleModule;
			this.simpleModule.NotifyEvent += SimpleModule_NotifyEvent;
			this.simpleModule.SubscriptionRemovedEvent += SimpleModule_SubscriptionRemovedEvent;

			this.notifyProducer = new ProducedRequest(this);
		}

		public void Dispose()
		{
			simpleModule.Dispose();
		}

		public override IEnumerable<ProducedRequest> GetProducedRequests()
		{
			yield return notifyProducer;
		}

		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			yield return new AcceptedRequest(this)
			{
				Method = Methods.Publishm,
				IsAcceptedRequest = (reader) =>
					AcceptedRequest.IsAccepted(reader, presence, application, pidfXml, true),
				IncomingRequest = ProccessPublish,
			};

			yield return new AcceptedRequest(this)
			{
				Method = Methods.Subscribem,
				IsAcceptedRequest = (reader) =>
					AcceptedRequest.IsAccepted(reader, presence, false),
				IncomingRequest = ProccessSubscribe,
			};
		}

		private void ProccessPublish(AcceptedRequest tu, IncomingMessageEx request)
		{
			StatusCodes statusCode = StatusCodes.OK;

			int expires = request.Reader.GetExpires(600, 900);

			if (request.Reader.IsExpiresTooBrief(60))
				statusCode = StatusCodes.IntervalTooBrief;

			int sipIfMatch = simpleModule.InvalidEtag;

			if (statusCode == StatusCodes.OK)
			{
				if (request.Reader.SipIfMatch.Length == 8)
					if (HexEncoding.TryParseHex8(request.Reader.SipIfMatch.Bytes, request.Reader.SipIfMatch.Begin, out sipIfMatch) == false)
						statusCode = StatusCodes.CallLegTransactionDoesNotExist;
			}

			if (statusCode == StatusCodes.OK)
			{
				var fromUser = request.Reader.From.AddrSpec.User.ToString();
				var fromHost = request.Reader.From.AddrSpec.Hostport.Host.ToString();
				if (simpleModule.Publish(fromUser + "@" + fromHost, ref sipIfMatch, expires, request.Content) == false)
					statusCode = StatusCodes.BadRequest;
			}

			var writer = GetWriter();

			if (statusCode == StatusCodes.OK)
			{
				writer.WriteStatusLine(statusCode);
				writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, statusCode);
				writer.WriteExpires(expires);
				writer.WriteSipEtag(sipIfMatch);
				writer.WriteCRLF();
			}
			else
			{
				writer.WriteResponse(request.Reader, statusCode);
			}

			SendResponse(request, writer);
		}

		private void ProccessSubscribe(AcceptedRequest tu, IncomingMessageEx request)
		{
			var dialog = dialogManager.GetOrCreate(request.Reader, request.ConnectionAddresses);

			if (dialog == null)
				SendResponse(request, StatusCodes.CallLegTransactionDoesNotExist);
			else if (request.Reader.IsExpiresTooBrief(60))
				SendResponse(request, StatusCodes.IntervalTooBrief);
			else
			{
				int expires = request.Reader.GetExpires(600, 900);


				var dialogId = dialog.Id.ToString();
				var toUser = request.Reader.To.AddrSpec.User.ToString();
				var toHost = request.Reader.To.AddrSpec.Hostport.Host.ToString();
				var document = simpleModule.Subscribe(dialogId, toUser + "@" + toHost, expires);


				var statusCode = StatusCodes.OK;
				var writer = GetWriter();

				writer.WriteStatusLine(statusCode);
				writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, statusCode, dialog.LocalTag);
				writer.WriteExpires(expires);
				writer.WriteCRLF();

				SendResponse(request, writer);


				if (document != null)
					SendNotify(dialog, expires, document);
			}
		}

		private void SimpleModule_NotifyEvent(string publisherId, string subscriptionId, int expires, PresenceDocument document)
		{
			var dialog = dialogManager.Get(new ByteArrayPart(subscriptionId));

			if (dialog != null)
				SendNotify(dialog, expires, document);
		}

		private void SimpleModule_SubscriptionRemovedEvent(string subscriptionId)
		{
			dialogManager.Remove(new ByteArrayPart(subscriptionId));
		}

		private void SendNotify(Dialog dialog, int expires, PresenceDocument document)
		{
			int transactionId = GetTransactionId(Methods.Notifym);

			var writer = new SipMessageWriter();

			writer.WriteRequestLine(Methods.Notifym, dialog.RemoteUri);
			writer.WriteVia(dialog.Transport, dialog.LocalEndPoint, transactionId);
			writer.WriteFrom(dialog.LocalUri, dialog.LocalTag);
			writer.WriteTo(dialog.RemoteUri, dialog.RemoteTag);
			writer.WriteCallId(dialog.CallId);
			writer.WriteEventPresence();
			writer.WriteSubscriptionState(expires);
			writer.WriteMaxForwards(70);
			writer.WriteCseq(dialog.GetNextLocalCseq(), Methods.Notifym);
			writer.WriteContact(dialog.LocalEndPoint, dialog.Transport);

			if (document != null)
			{
				writer.WriteContentType(application, pidfXml);
				//writer.WriteContentLength();
				//writer.WriteCRLF();

				//writer.RewriteContentLength(
				//    document.CopyTo((length) => writer.GetBytesForCustomWrite(length)));

				document.WriteLenghtAndContent(writer);
			}
			else
			{
				writer.WriteContentLength(0);
				writer.WriteCRLF();
			}

			notifyProducer.SendRequest(dialog.Transport, dialog.LocalEndPoint,
				dialog.RemoteEndPoint, ServerAsyncEventArgs.AnyConnectionId, writer, transactionId);
		}
	}
}

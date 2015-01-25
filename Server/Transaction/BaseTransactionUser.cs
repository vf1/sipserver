using System;
using System.Net;
using System.Collections.Generic;
using Sip.Message;
using SocketServers;
using Server.Authorization.Sip;

namespace Sip.Server
{
	abstract class BaseTransactionUser
	{
		public delegate void SendRequestDelegate(int index, ConnectionAddresses connectionAddresses, int transactionId, SipMessageWriter writer, int userData);
		public delegate void SendResponseDelegate(ConnectionAddresses connectionAddresses, int transactionId, SipMessageWriter writer);
		public delegate void SendNonTransactionMessageDelegate(Transports transport, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, int connectionId, SipMessageWriter writer);

		public abstract IEnumerable<AcceptedRequest> GetAcceptedRequests();

		public virtual IEnumerable<ProducedRequest> GetProducedRequests()
		{
			return new ProducedRequest[0];
		}

		public SendResponseDelegate SendResponseExternal;
		public SendRequestDelegate SendRequest;
		public SendNonTransactionMessageDelegate SendNonTransactionMessage;
		public Func<SipResponseWriter> GetWriter;
		public Func<Methods, int> GetTransactionId;
		public Func<IPAddress, bool> IsLocalAddress;

		public ISipAuthorizationManager Authentication { get; set; }

		public virtual AuthorizationMode OnCustomAuthorization(IncomingMessageEx message)
		{
			return AuthorizationMode.Disabled;
		}

		public void OnIncomingRequest(AcceptedRequest router, IncomingMessageEx message)
		{
			bool repeat;
			var mode = router.AuthorizationMode;

			do
			{
				repeat = mode == AuthorizationMode.Custom;

				switch (mode)
				{
					case AuthorizationMode.Custom:
						{
							mode = OnCustomAuthorization(message);
						}
						break;

					case AuthorizationMode.Disabled:
						{
							router.OnIncomingRequest(message);
						}
						break;

					case AuthorizationMode.Enabled:
						{
							SipMessageWriter writer;
							if (Authentication.IsAuthorized(message.Reader, message.Content, out writer))
								router.OnIncomingRequest(message);
							else if (writer != null)
								SendResponse(message, writer);
						}
						break;
				}
			}
			while (repeat);
		}

		public void OnIncomingResponse(ProducedRequest router, IncomingMessageEx message)
		{
			router.OnIncomingResponse(message);
		}

		protected SipMessageWriter GenerateResponse(SipMessageReader reader, StatusCodes statusCode)
		{
			var writer = GetWriter();
			writer.WriteResponse(reader, statusCode);

			return writer;
		}

		protected void SendResponse(IncomingMessageEx request, StatusCodes statusCode)
		{
			SendResponseExternal(request.ConnectionAddresses, request.TransactionId,
				GenerateResponse(request.Reader, statusCode));
		}

		protected void SendResponse(IncomingMessageEx to, SipMessageWriter writer)
		{
			SendResponseExternal(to.ConnectionAddresses, to.TransactionId, writer);
		}

		public void SendResponse(ConnectionAddresses connectionAddresses, int transactionId, SipMessageWriter writer)
		{
			SendResponseExternal(connectionAddresses, transactionId, writer);
		}

		protected void SendAck(ConnectionAddresses connectionAddresses, SipMessageWriter writer)
		{
			SendNonTransactionMessage(connectionAddresses.Transport, connectionAddresses.LocalEndPoint,
				connectionAddresses.RemoteEndPoint, connectionAddresses.ConnectionId, writer);
		}
	}
}

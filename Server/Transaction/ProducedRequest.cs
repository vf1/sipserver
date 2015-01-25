using System;
using System.Net;
using Sip.Message;
using SocketServers;

namespace Sip.Server
{
	sealed class ProducedRequest
	{
		public int Index { get; set; }
		public Action<IncomingMessageEx> IncomingResponse { get; set; }
		public Action<IncomingMessageEx> ProduceAck { get; set; }
		public Action<int, int> TransportError { get; set; }
		public BaseTransactionUser TransactionUser { get; private set; }

		public ProducedRequest(BaseTransactionUser transactionUser)
		{
			TransactionUser = transactionUser;
			IncomingResponse = None;
			TransportError = None;
			ProduceAck = None;
		}

		public void OnIncomingResponse(IncomingMessageEx message)
		{
			IncomingResponse(message);
		}

		//public int GetTransactionId(Methods method)
		//{
		//    return TransactionUser.GetTransactionId(method);
		//}

		public void SendRequest(ConnectionAddresses connectionAddresses, SipMessageWriter writer, int transactionId, int userData)
		{
			TransactionUser.SendRequest(Index, connectionAddresses, transactionId, writer, userData);
		}

		public void SendRequest(ConnectionAddresses connectionAddresses, SipMessageWriter writer, int transactionId)
		{
			TransactionUser.SendRequest(Index, connectionAddresses, transactionId, writer, 0);
		}

		public void SendRequest(Transports transport, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, int connectionId, SipMessageWriter writer, int transactionId)
		{
			TransactionUser.SendRequest(Index,
				new ConnectionAddresses(transport, localEndPoint, remoteEndPoint, connectionId), transactionId, writer, 0);
		}

		private static void None(IncomingMessageEx message) { }
		private static void None(int transactionId, int userData) { }
	}
}

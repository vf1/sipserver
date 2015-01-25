using System;
using System.Net;
using System.Net.Sockets;
using Sip.Message;
using SocketServers;

namespace Sip.Server
{
	public struct IncomingMessageEx
	{
		public IncomingMessageEx(IncomingMessage source, int transactionId)
		{
			TransactionId = transactionId;

			ConnectionAddresses = source.ConnectionAddresses;

			Reader = source.Reader;

			Headers = source.Header;
			Content = source.Content;
		}

		public readonly int TransactionId;

		public readonly ConnectionAddresses ConnectionAddresses;

		public readonly SipMessageReader Reader;
		public readonly ArraySegment<byte> Headers;
		public readonly ArraySegment<byte> Content;

		public BufferHandle DetachBuffers()
		{
			return TransportLayer.DetachBuffers();
		}

		#region Accessors to ConnectionAddresses fields

		public Transports Transport
		{
			get { return ConnectionAddresses.Transport; }
		}

		public IPEndPoint LocalEndPoint
		{
			get { return ConnectionAddresses.LocalEndPoint; }
		}

		public IPEndPoint RemoteEndPoint
		{
			get { return ConnectionAddresses.RemoteEndPoint; }
		}

		public int ConnectionId
		{
			get { return ConnectionAddresses.ConnectionId; }
		}

		public bool IsTransportUnreliable
		{
			get { return ConnectionAddresses.Transport == Transports.Udp; }
		}

		#endregion
	}
}

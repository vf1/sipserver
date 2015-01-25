using System;
using System.Net;
using System.Net.Sockets;
using Sip.Message;
using SocketServers;

namespace Sip.Server
{
	public struct IncomingMessage
	{
		public readonly ConnectionAddresses ConnectionAddresses;
		public readonly SipMessageReader Reader;
		public readonly ArraySegment<byte> Header;
		public readonly ArraySegment<byte> Content;

		public IncomingMessage(Connection connection)
		{
			ConnectionAddresses = new ConnectionAddresses(GetTransport(connection), connection.LocalEndPoint,
				new IPEndPoint(connection.RemoteEndPoint.Address, connection.RemoteEndPoint.Port), connection.Id);

			Reader = connection.Reader;
			Header = connection.Header;
			Content = connection.Content;
		}

		public IncomingMessage(ServerAsyncEventArgs e, SipMessageReader reader, ArraySegment<byte> content)
		{
			if (e.LocalEndPoint.Protocol != ServerProtocol.Udp)
				throw new ArgumentException();

			ConnectionAddresses = new ConnectionAddresses(Transports.Udp, e.LocalEndPoint,
				new IPEndPoint(e.RemoteEndPoint.Address, e.RemoteEndPoint.Port), ServerAsyncEventArgs.AnyNewConnectionId);

			Reader = reader;
			Content = content;
			Header = new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred - content.Count);
		}

		public IncomingMessage(Transports transport, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, int connectionId, SipMessageReader reader, ArraySegment<byte> readerData, ArraySegment<byte> contentData)
		{
			ConnectionAddresses = new ConnectionAddresses(transport, localEndPoint, remoteEndPoint, connectionId);

			Reader = reader;
			Header = readerData;
			Content = contentData;
		}

		public IncomingMessage(ConnectionAddresses connectionAddresses, Connection connection)
		{
			ConnectionAddresses = connectionAddresses;

			Reader = connection.Reader;
			Header = connection.Header;
			Content = connection.Content;
		}

		public static Transports GetTransport(Connection connection)
		{
			switch (connection.LocalEndPoint.Protocol)
			{
				case ServerProtocol.Tcp:
					return (connection.Mode == Connection.Modes.WebSocket || connection.Mode == Connection.Modes.Ajax)
						? Transports.Ws : Transports.Tcp;

				case ServerProtocol.Tls:
					return (connection.Mode == Connection.Modes.WebSocket || connection.Mode == Connection.Modes.Ajax)
						? Transports.Wss : Transports.Tls;

				case ServerProtocol.Udp:
				default:
					throw new ArgumentException();
			}
		}
	}
}

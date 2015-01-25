using System;
using System.Net;
using System.Net.Sockets;
using SocketServers;
using Sip.Message;
using Server.Http;

namespace Sip.Server
{
	public sealed class TransportLayer
		: IDisposable
	{
		[ThreadStatic]
		private static Connection connection;
		[ThreadStatic]
		private static ServerAsyncEventArgs e;
		[ThreadStatic]
		private static SipMessageReader readerUdp;

		private readonly ServersManager<Connection> serversManager;
		private readonly AjaxWebsocket ajax;

		public event Action<IncomingMessage> IncomingMessage;
		public event Action<int> SendErrorSip;
		public Action<Connection> IncomingHttpRequest;

		private const int SipModule = 1;
		private const int HttpModule = 2;
		private const int AjaxModule = 3;

		private Opcodes? websocketOpcode;

		public TransportLayer(ServersManager<Connection> serversManager1, Opcodes? websocketResponseFrame)
		{
			ChangeSettings(websocketResponseFrame);

			serversManager = serversManager1;
			serversManager.ServerAdded += ServersManager_ServerAdded;
			serversManager.ServerRemoved += ServersManager_ServerRemoved;
			serversManager.ServerInfo += ServersManager_ServerInfo;
			serversManager.NewConnection += ServersManager_NewConnection;
			serversManager.EndConnection += ServersManager_EndConnection;
			serversManager.Received += ServersManager_Received;
			serversManager.Sent += ServersManager_Sent;
			serversManager.BeforeSend += ServersManager_BeforeSend;

			ajax = new AjaxWebsocket();
			ajax.SendAsync = SendAsyncAjax;
		}

		public void ChangeSettings(Opcodes? websocketResponseFrame)
		{
			this.websocketOpcode = websocketResponseFrame;
		}

		public void Dispose()
		{
			serversManager.Dispose();
		}

		public void Start()
		{
			serversManager.Start(true);
		}

		public void SendAsyncSip(ServerAsyncEventArgs e)
		{
			if (AjaxWebsocket.IsAjax(e))
				ajax.ProcessOutgoing(e);
			else
			{
				e.UserTokenForSending2 = SipModule;
				serversManager.SendAsync(e);
			}
		}

		public void SendAsyncHttp(ServerAsyncEventArgs e, int httpModuleId)
		{
			e.UserTokenForSending2 = HttpModule | (httpModuleId << 8);
			serversManager.SendAsync(e);
		}

		private void SendAsyncAjax(ServerAsyncEventArgs e, bool sipAjax)
		{
			e.UserTokenForSending2 = sipAjax ? SipModule : AjaxModule;
			serversManager.SendAsync(e);
		}

		public bool IsLocalAddress(IPAddress address)
		{
			return serversManager.IsLocalAddress(address);
		}

		public static BufferHandle DetachBuffers()
		{
			if (connection != null)
				return connection.Dettach(ref e);
			else if (e != null)
				return new BufferHandle(e.DetachBuffer(), new ArraySegment<byte>());

			return new BufferHandle();
		}

		private void ServersManager_ServerAdded(object sender, ServerChangeEventArgs e)
		{
			Tracer.WriteInformation(string.Format(@"Added: {0}", e.ServerEndPoint.ToString()));
		}

		private void ServersManager_ServerRemoved(object sender, ServerChangeEventArgs e)
		{
			Tracer.WriteInformation(string.Format(@"Removed: {0}", e.ServerEndPoint.ToString()));
		}

		private void ServersManager_ServerInfo(object sender, ServerInfoEventArgs e)
		{
			Tracer.WriteInformation(string.Format(@"Info: {0}, {1}", e.ServerEndPoint.ToString(), e.ToString()));
		}

		private void ServersManager_NewConnection(ServersManager<Connection> s, Connection c)
		{
			Tracer.WriteInformation(string.Format(@"New Connection from {0}", c.RemoteEndPoint.ToString()));
		}

		private void ServersManager_EndConnection(ServersManager<Connection> s, Connection c)
		{
			Tracer.WriteInformation(string.Format(@"End Connection from {0}", c.RemoteEndPoint.ToString()));
		}

		private bool ServersManager_Received(ServersManager<Connection> s, Connection connection1, ref ServerAsyncEventArgs e1)
		{
			connection = connection1;
			e = e1;

			bool closeConnection = OnReceived();

			e1 = e;

			connection = null;
			e = null;

			return closeConnection;
		}

		private bool OnReceived()
		{
			bool closeConnection = false;

			switch (e.LocalEndPoint.Protocol)
			{
				case ServerProtocol.Udp:
					{
						if (readerUdp == null)
							readerUdp = new SipMessageReader();

						readerUdp.SetDefaultValue();

						int parsed = readerUdp.Parse(e.Buffer, e.Offset, e.BytesTransferred);

						if (readerUdp.IsFinal)
						{
							if (readerUdp.HasContentLength == false)
								readerUdp.ContentLength = e.BytesTransferred - parsed;

							if (readerUdp.ContentLength == e.BytesTransferred - parsed)
							{
								readerUdp.SetArray(e.Buffer);

								OnIncomingSipMessage(new IncomingMessage(e, readerUdp,
									new ArraySegment<byte>(e.Buffer, e.Offset + parsed, readerUdp.ContentLength)));
							}
						}
					}
					break;


				case ServerProtocol.Tcp:
				case ServerProtocol.Tls:
					{
						for (bool repeat = true; repeat && closeConnection == false; )
						{
							repeat = connection.Proccess(ref e, out closeConnection);

							if (connection.IsMessageReady)
							{
								switch (connection.Mode)
								{
									case Connection.Modes.WebSocket:
										if (connection.IsSipWebSocket)
											goto case Connection.Modes.Sip;
										OnIncomingWebSocketMessage(connection, out closeConnection);
										break;

									case Connection.Modes.Sip:
										OnIncomingSipMessage(new IncomingMessage(connection));
										break;

									case Connection.Modes.Http:
										OnIncominHttpRequest(connection);
										break;

									case Connection.Modes.Ajax:
										int id = ajax.ProcessConnection(connection);
										if (id >= 0 && connection.Reader.IsFinal)
											OnIncomingSipMessage(new IncomingMessage(ajax.GetConnectionAddresses(connection, id), connection));
										break;

									default:
										throw new InvalidProgramException();
								}

								connection.ResetState();
							}
						}
					}
					break;

				default:
					throw new NotImplementedException();
			}

			return !closeConnection;
		}

		private void ServersManager_BeforeSend(ServersManager<Connection> s, Connection c, ServerAsyncEventArgs e)
		{
			if (c != null && c.Mode == Connection.Modes.WebSocket)
			{
				//c.BeforeSend(e);

				var header = new WebSocketHeader()
				{
					Fin = true,
					Opcode = websocketOpcode.HasValue ? websocketOpcode.Value : c.WebSocketHeader.Opcode,
					PayloadLength = e.Count,
				};

				int headerLength = header.GetHeaderLength();

				if (e.OffsetOffset < headerLength)
					throw new InvalidProgramException(@"TransportLayer.ServersManager_BeforeSend no reserved space for WebSocket header");

				e.OffsetOffset -= headerLength;
				e.Count += headerLength;

				header.GenerateHeader(e.OutgoingData);
			}
		}

		private void ServersManager_Sent(ServersManager<Connection> s, ref ServerAsyncEventArgs e)
		{
			if (e.SocketError != SocketError.Success)
			{
				Tracer.WriteInformation("Send error");

				switch (e.UserTokenForSending2 & 0x000000ff)
				{
					case SipModule:
						SendErrorSip(e.UserTokenForSending);
						break;
					case HttpModule:
						// e.UserTokenForSending2 >> 8 == httpModuleId
						break;
					case AjaxModule:
						ajax.SendError(e.UserTokenForSending);
						break;
				}
			}
		}

		private void OnIncomingSipMessage(IncomingMessage message)
		{
			IncomingMessage(message);
		}

		private void OnIncominHttpRequest(Connection connection)
		{
			IncomingHttpRequest(connection);
		}

		private void OnIncomingWebSocketMessage(Connection connection, out bool closeConnection)
		{
			closeConnection = false;

			if (connection.WebSocketHeader.Opcode == Opcodes.Ping)
			{
				SendWebsocket(
					new WebSocketHeader()
					{
						Fin = true,
						Opcode = Opcodes.Pong,
						PayloadLength = connection.Content.Count,
					},
					connection.Content);
			}

			if (connection.WebSocketHeader.Opcode == Opcodes.ConnectionClose)
			{
				closeConnection = true;
			}
		}

		private void SendWebsocket(WebSocketHeader header, ArraySegment<byte> content)
		{
			int headerLength = header.GetHeaderLength();

			var r = EventArgsManager.Get();
			r.CopyAddressesFrom(connection);
			r.Count = headerLength + content.Count;
			r.AllocateBuffer();

			header.GenerateHeader(r.OutgoingData);
			Buffer.BlockCopy(content.Array, content.Offset, r.Buffer, r.Offset + headerLength, content.Count);

			SendAsyncSip(r);
		}
	}
}

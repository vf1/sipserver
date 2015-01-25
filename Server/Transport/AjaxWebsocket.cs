using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using SocketServers;
using Http.Message;
using Sip.Tools;

namespace Sip.Server
{
	sealed class AjaxWebsocket
	{
		#region class Session {...}

		class Session
		{
			public readonly int Id;
			public const int InvalidId = -1;

			private int connectionId;
			private ServerEndPoint localEndPoint;
			private IPEndPoint remoteEndPoint;
			private List<ServerAsyncEventArgs> outgoingMessages;

			public int TimerId;

			public Session(int id)
			{
				Id = id;
				outgoingMessages = new List<ServerAsyncEventArgs>();
			}

			public ServerAsyncEventArgs GetOutgoingMessage()
			{
				ServerAsyncEventArgs e = null;

				if (outgoingMessages.Count > 0)
				{
					e = outgoingMessages[0];
					outgoingMessages.RemoveAt(0);
				}

				return e;
			}

			public void AddOutgoingMessage(ServerAsyncEventArgs e)
			{
				outgoingMessages.Add(e);
			}

			public void StoreAddresses(Connection connection)
			{
				connectionId = connection.Id;
				localEndPoint = connection.LocalEndPoint;
				remoteEndPoint = connection.RemoteEndPoint;
			}

			public void RestoreAddresses(ServerAsyncEventArgs e)
			{
				e.ConnectionId = connectionId;
				e.LocalEndPoint = localEndPoint;
				e.RemoteEndPoint = remoteEndPoint;

				localEndPoint = null;
				remoteEndPoint = null;
			}

			public bool IsConnected
			{
				get { return localEndPoint != null; }
			}

			public int Cookie
			{
				get { return Id & 0xffff; }
			}

			public static int GetSessionId(int clientId, int cookie)
			{
				return (clientId << 16) | cookie;
			}
		}

		#endregion

		private static readonly byte[] ajaxUri = Encoding.UTF8.GetBytes("/ajax.websocket");
		private static readonly byte[] cookieName = Encoding.UTF8.GetBytes("sessionid");
		private static readonly IPEndPoint ajaxEndPoint = new IPEndPoint(IPAddress.Any, 12345);

		private readonly ThreadSafeDictionary<int, Session> sessions;
		private readonly MultiTimer<int> timer;

		private int sessionCount;

		public Action<ServerAsyncEventArgs, bool> SendAsync;

		public AjaxWebsocket()
		{
			sessions = new ThreadSafeDictionary<int, Session>();
			timer = new MultiTimer<int>(KeepConnectionTimer, 1024, 20000);
		}

		public static bool IsAjax(HttpMessageReader rawReader, byte[] array)
		{
			if (rawReader.RequestUri.Length < ajaxUri.Length)
				return false;

			for (int i = 0; i < ajaxUri.Length; i++)
				if (ajaxUri[i] != array[rawReader.RequestUri.Begin + i])
					return false;

			return true;
		}

		public static bool IsAjax(ServerAsyncEventArgs e)
		{
			return (e.LocalEndPoint as IPEndPoint).Equals(ajaxEndPoint);
		}

		public ConnectionAddresses GetConnectionAddresses(Connection connection, int sessionId)
		{
			return new ConnectionAddresses(IncomingMessage.GetTransport(connection), ajaxEndPoint, connection.RemoteEndPoint, sessionId);
		}

		public int ProcessConnection(Connection connection)
		{
			Session session = null;

			int sessionId = GetSessionId(connection.HttpReader);
			if (sessionId == Session.InvalidId)
			{
				if (connection.HttpReader.Method == Methods.Get)
				{
					session = new Session(GenerateSessionId(connection.HttpReader));
					session.StoreAddresses(connection);

					lock (session)
					{
						sessions.Add(session.Id, session);
						SendJsonMessage(session);
					}
				}
				else
				{
					SendResponse(connection, StatusCodes.NotAcceptable);
				}
			}
			else if (sessions.TryGetValue(sessionId, out session))
			{
				if (connection.HttpReader.Method == Methods.Get)
				{
					lock (session)
					{
						if (session.IsConnected)
							SendEmptyMessage(session);

						session.StoreAddresses(connection);

						var e = session.GetOutgoingMessage();

						if (e != null)
							SendSipEventArgs(e, session);
						else
							session.TimerId = timer.Add(session.Id);
					}
				}
				else if (connection.HttpReader.Method == Methods.Post)
				{
					SendResponse(connection, StatusCodes.OK);
				}
				else
				{
					session = null;
					SendResponse(connection, StatusCodes.NotAcceptable);
				}
			}
			else
			{
				SendResponse(connection, StatusCodes.Gone);
			}

			return (session != null) ? session.Id : Session.InvalidId;
		}

		public void ProcessOutgoing(ServerAsyncEventArgs e)
		{
			int sessionId = e.ConnectionId;

			Session session;
			if (sessions.TryGetValue(sessionId, out session) == false)
			{
				// send error
			}
			else
			{
				lock (session)
				{
					if (session.IsConnected)
						SendSipEventArgs(e, session);
					else
						session.AddOutgoingMessage(e);
				}
			}
		}

		public void SendError(int userTokenForSending)
		{
		}

		private void KeepConnectionTimer(int timerId, int sessionId)
		{
			Session session;
			if (sessions.TryGetValue(sessionId, out session))
			{
				lock (session)
				{
					if (session.TimerId == timerId && session.IsConnected)
						SendEmptyMessage(session);
				}
			}
		}

		private int GenerateSessionId(HttpMessageReader httpReader)
		{
			int newSessionCount, oldSessionCount;
			do
			{
				oldSessionCount = sessionCount;
				newSessionCount = (oldSessionCount < int.MaxValue) ? oldSessionCount + 1 : 1;
			}
			while (Interlocked.CompareExchange(ref sessionCount, newSessionCount, oldSessionCount) != oldSessionCount);

			return newSessionCount;
		}

		private static int GetSessionId(HttpMessageReader httpReader)
		{
			var prefix = @"id=";
			var requestUri = httpReader.RequestUri.ToString();

			int s = requestUri.IndexOf(prefix);
			if (s >= 0)
			{
				s += prefix.Length;

				int result = 0;
				for (int i = s; i < requestUri.Length; i++)
					if (requestUri[i] >= '0' && requestUri[i] <= '9')
					{
						result *= 10;
						result += requestUri[i] - '0';
					}
					else
					{
						break;
					}

				return (result == 0) ? Session.InvalidId : result;
			}

			return Session.InvalidId;
		}

		//private bool IsInitialRequest(HttpMessageReader httpReader)
		//{
		//    return httpReader.RequestUri.ToString().Contains("connect=1");
		//}

		//private int GetSessionId(HttpMessageReader httpReader)
		//{
		//    uint cookie;
		//    if (httpReader.TryGetCookieUInt(cookieName, out cookie))
		//        return Session.GetSessionId(GetConnectionId(httpReader), (int)cookie);

		//    return -1;
		//}

		private void SendSipEventArgs(ServerAsyncEventArgs e, Session session)
		{
			session.RestoreAddresses(e);

			WriteHttpHeader(e);
			SendAsync(e, true);
		}

		private static void WriteHttpHeader(ServerAsyncEventArgs e)
		{
			using (var writer = new HttpMessageWriter())
			{
				writer.WriteStatusLine(StatusCodes.OK);
				writer.WriteContentLength(e.Count);
				writer.WriteAccessControlHeaders();
				writer.WriteCRLF();

				e.OffsetOffset -= writer.Count;
				e.Count += writer.Count;
				Buffer.BlockCopy(writer.Buffer, writer.Offset, e.Buffer, e.Offset, writer.Count);
			}
		}

		private void SendJsonMessage(Session session)
		{
			using (var writer = new HttpMessageWriter())
			{
				//byte[] json = Encoding.UTF8.GetBytes(string.Format(@"{{id:{0}}}", session.Id));
				byte[] json = Encoding.UTF8.GetBytes(string.Format(@"{0}", session.Id));

				writer.WriteStatusLine(StatusCodes.OK);
				//writer.Write(Encoding.UTF8.GetBytes("Content-Type: application/json\r\n"));
				writer.WriteContentLength(json.Length);
				writer.WriteAccessControlHeaders();
				writer.WriteCRLF();
				writer.Write(json);

				SendWriter(session, writer);
			}
		}

		private void SendEmptyMessage(Session session)
		{
			using (var writer = new HttpMessageWriter())
			{
				writer.WriteEmptyResponse(StatusCodes.OK);

				SendWriter(session, writer);
			}
		}

		private void SendResponse(Connection connection, StatusCodes statusCode)
		{
			using (var writer = new HttpMessageWriter())
			{
				writer.WriteEmptyResponse(statusCode);

				SendWriter(connection, writer);
			}
		}

		private void SendWriter(Connection connection, HttpMessageWriter writer)
		{
			var r = EventArgsManager.Get();
			r.CopyAddressesFrom(connection);

			SendWriter(r, writer);
		}

		private void SendWriter(Session session, HttpMessageWriter writer)
		{
			var r = EventArgsManager.Get();
			session.RestoreAddresses(r);

			SendWriter(r, writer);
		}

		private void SendWriter(ServerAsyncEventArgs r, HttpMessageWriter writer)
		{
			r.Count = writer.Count;
			r.OffsetOffset = writer.OffsetOffset;
			r.AttachBuffer(writer.Detach());

			SendAsync(r, false);
		}
	}
}

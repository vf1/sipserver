using System;
using System.Text;
using System.Collections.Generic;
using Base.Message;
using Http.Message;
using SocketServers;

namespace Server.Http
{
	class BaseHttpServer
		: IDisposable
		, IHttpServerAgentRegistrar
	{
		#region struct Agent {...}

		struct Agent
		{
			public readonly int Priority;
			public readonly IHttpServerAgent Value;
			public readonly bool IsAuthorizationEnabled;

			public Agent(IHttpServerAgent agent, int priority, bool isAuthorizationEnabled)
			{
				Value = agent;
				Priority = priority;
				IsAuthorizationEnabled = isAuthorizationEnabled;
			}
		}

		#endregion

		#region struct HttpServerWrapper {...}

		struct HttpServerWrapper
			: IHttpServer
		{
			private int index;
			private BaseHttpServer server;

			public HttpServerWrapper(BaseHttpServer server, int index)
			{
				this.server = server;
				this.index = index;
			}

			HttpMessageWriter IHttpServer.GetHttpMessageWriter()
			{
				return server.GetHttpMessageWriter();
			}

			void IHttpServer.SendResponse(BaseConnection connection, HttpMessageWriter writer)
			{
				server.SendResponse(connection, writer, index);
			}

			void IHttpServer.SendResponse(BaseConnection connection, ArraySegment<byte> data)
			{
				server.SendResponse(connection, data, index);
			}
		}

		#endregion

		private Agent[] agents;
		private readonly byte[] defaultUri;
		protected const int InvalidAgentIndex = 0xff;

		public Action<ServerAsyncEventArgs, int> SendAsync;

		public BaseHttpServer(string defaultUri)
		{
			this.agents = new Agent[0];
			this.defaultUri = Encoding.UTF8.GetBytes(defaultUri);
		}

		public virtual void Dispose()
		{
			if (agents != null)
			{
				for (int i = 0; i < agents.Length; i++)
					agents[i].Value.Dispose();
				agents = null;
			}
		}

		public void Register(IHttpServerAgent agent, int priority, bool isAuthorizationEnabled)
		{
			int index = 0;
			for (; index < agents.Length; index++)
				if (agents[index].Priority > priority)
					break;

			Array.Resize<Agent>(ref agents, agents.Length + 1);
			Array.Copy(agents, index, agents, index + 1, agents.Length - 1 - index);

			agents[index] = new Agent(agent, priority, isAuthorizationEnabled);

			agent.IHttpServer = new HttpServerWrapper(this, index);
		}

		public void ProcessIncomingRequest(BaseHttpConnection c)
		{
			bool handled = false;

			for (int i = 0; i < agents.Length; i++)
			{
				var result = agents[i].Value.IsHandled(c.HttpReader);
				if (result.IsHandled)
				{
					bool isAuthorized = true;

					if (agents[i].IsAuthorizationEnabled && result.IsAuthorizationRequred)
					{
						var writer = IsAuthorized(c, result.Realm, i);
						if (writer != null)
						{
							isAuthorized = false;
							SendResponse(c, writer, InvalidAgentIndex);
						}
					}

					if (isAuthorized)
						agents[i].Value.HandleRequest(c, c.HttpReader, c.Content);

					handled = true;

					break;
				}
			}

			if (handled == false)
			{
				var writer = new HttpMessageWriter();

				writer.WriteStatusLine(StatusCodes.TemporaryRedirect);
				writer.WriteLocation(c.LocalEndPoint.Protocol == ServerProtocol.Tcp, c.HttpReader.Host.Host, c.HttpReader.Host.Port, defaultUri);
				writer.WriteContentLength(0);
				writer.WriteCRLF();

				SendResponse(c, writer, InvalidAgentIndex);
			}
		}

		protected virtual HttpMessageWriter IsAuthorized(BaseHttpConnection c, ByteArrayPart realm, int agentIndex)
		{
			return null;
		}

		protected bool IsAuthorizedByAgent(HttpMessageReader reader, ByteArrayPart username, int agentIndex)
		{
			return agents[agentIndex].Value.IsAuthorized(reader, username);
		}

		private HttpMessageWriter GetHttpMessageWriter()
		{
			return new HttpMessageWriter();
		}

		protected void SendResponse(BaseConnection c, HttpMessageWriter writer, int agentIndex)
		{
			var r = EventArgsManager.Get();
			r.CopyAddressesFrom(c);
			r.Count = writer.Count;
			r.OffsetOffset = writer.OffsetOffset;
			r.AttachBuffer(writer.Detach());

			SendAsync(r, agentIndex);
		}

		protected void SendResponse(BaseConnection c, ArraySegment<byte> data, int agentIndex)
		{
			int offset = data.Offset, left = data.Count;

			while (left > 0)
			{
				var r = EventArgsManager.Get();
				r.CopyAddressesFrom(c);
				r.OffsetOffset = r.MinimumRequredOffsetOffset;
				r.AllocateBuffer();
				r.SetMaxCount();

				if (r.Count > left)
					r.Count = left;

				r.BlockCopyFrom(new ArraySegment<byte>(data.Array, offset, r.Count));

				offset += r.Count;
				left -= r.Count;

				SendAsync(r, agentIndex);
			}
		}
	}
}

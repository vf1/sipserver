using System;
using System.IO;
using System.Text;
using System.Timers;
using System.Collections.Generic;
using System.Security.Cryptography;
using Base.Message;
using Http.Message;
using SocketServers;
using Sip.Server;
using Sip.Server.Configuration;
using Server.Authorization.Http;

namespace Server.Http
{
	class HttpServer
		: BaseHttpServer
		, IDisposable
		, IHttpServerAgentRegistrar
	{
		private const string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
		private readonly SHA1CryptoServiceProvider sha1;
		private readonly byte[] sip;
		private readonly IHttpAuthorizationManager authorization;

		public HttpServer(IHttpAuthorizationManager authorization, string defaultUri)
			: base(defaultUri)
		{
			this.sip = Encoding.UTF8.GetBytes(@"sip");
			this.sha1 = new SHA1CryptoServiceProvider();

			this.authorization = authorization;
			this.authorization.ValidateAuthorization = IsAuthorizedByAgent;
		}

		public override void Dispose()
		{
			base.Dispose();
			sha1.Clear();
		}

		public void ProcessIncomingRequest(Connection c)
		{
			if (c.HttpReader.HasUpgarde(Upgrades.Websocket))
			{
				// c.HttpReader.SecWebSocketProtocol

				var key = c.HttpReader.SecWebSocketKey.ToString();
				byte[] accept;
				lock (sha1)
					accept = sha1.ComputeHash(Encoding.ASCII.GetBytes(key + guid));

				using (var writer = new HttpMessageWriter())
				{
					writer.WriteStatusLine(StatusCodes.SwitchingProtocols);
					writer.WriteConnectionUpgrade();
					writer.WriteUpgradeWebsocket();
					writer.WriteSecWebSocketAccept(accept);
					writer.WriteSecWebSocketProtocol(sip);
					writer.WriteContentLength(0);
					writer.WriteCRLF();

					SendResponse(c, writer, InvalidAgentIndex);
				}

				c.UpgradeToWebsocket();
			}
			else
			{
				base.ProcessIncomingRequest(c);
			}
		}

		protected override HttpMessageWriter IsAuthorized(BaseHttpConnection c, ByteArrayPart realm, int agentIndex)
		{
			HttpMessageWriter writer;
			authorization.IsAuthorized(c.HttpReader, c.Content, realm, agentIndex, out writer);

			return writer;
		}

		private HttpMessageWriter GetHttpMessageWriter()
		{
			return new HttpMessageWriter();
		}
	}
}

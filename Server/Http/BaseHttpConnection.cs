using System;
using Http.Message;
using SocketServers;

namespace Server.Http
{
	public abstract class BaseHttpConnection
		: HeaderContentConnection
	{
		public abstract HttpMessageReader HttpReader { get; }
	}
}
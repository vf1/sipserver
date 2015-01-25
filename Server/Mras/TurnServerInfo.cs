using System;

namespace Mras
{
	public class TurnServerInfo
	{
		public string Fqdn { get; set; }
		public ushort UdpPort { get; set; }
		public ushort TcpPort { get; set; }
	}
}

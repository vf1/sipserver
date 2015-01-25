using System;
using System.Net;
using Sip.Message;
using SocketServers;

static class IpEndPointExt
{
	public static bool IsTransportUnreliable(this ServerEndPoint endPoint)
	{
		return endPoint.Protocol == ServerProtocol.Udp;
	}

	public static ServerProtocol ToServerProtocol(this Transports transport)
	{
		switch (transport)
		{
			case Transports.Udp:
				return ServerProtocol.Udp;
			case Transports.Tcp:
			case Transports.Ws:
				return ServerProtocol.Tcp;
			case Transports.Tls:
			case Transports.Wss:
				return ServerProtocol.Tls;
			default:
				throw new NotImplementedException("Can not convert transport " + transport.ToString() + " to protocol.");
		}

		throw new InvalidCastException();
	}

	public static Transports ToTransport(this ServerProtocol protocol)
	{
		switch (protocol)
		{
			case ServerProtocol.Udp:
				return Transports.Udp;
			case ServerProtocol.Tcp:
				return Transports.Tcp;
			case ServerProtocol.Tls:
				return Transports.Tls;
		}

		throw new InvalidCastException();
	}

	public static void CopyFrom(this IPEndPoint to, IPEndPoint from)
	{
		to.Address = from.Address;
		to.Port = from.Port;
	}

	public static IPEndPoint MakeCopy(this IPEndPoint from)
	{
		return new IPEndPoint(from.Address, from.Port);
	}

	public static bool IsEqual(this IPEndPoint ip1, IPEndPoint ip2)
	{
		return
			ip1.AddressFamily == ip2.AddressFamily &&
			ip1.Port == ip2.Port &&
			ip1.Address.Equals(ip2.Address);
	}
}

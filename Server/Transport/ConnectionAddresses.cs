using System;
using System.Net;
using System.Net.Sockets;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	public struct ConnectionAddresses
	{
		public ConnectionAddresses(Transports transport, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, int connectionId)
		{
			Transport = transport;
			LocalEndPoint = localEndPoint;
			RemoteEndPoint = remoteEndPoint;
			ConnectionId = connectionId;
		}

		public readonly Transports Transport;
		public readonly IPEndPoint LocalEndPoint;
		public readonly IPEndPoint RemoteEndPoint;
		public readonly int ConnectionId;

		//[ThreadStatic]
		//private static byte[] bytes;
		//[ThreadStatic]
		//private static byte[] chars;

		//private static void CreateThreadStaticVariables()
		//{
		//    if (bytes == null)
		//        bytes = new byte[256];
		//    if (chars == null)
		//        chars = new byte[512];
		//}

		//public ArraySegment<byte> ToLowerHexChars(int userData)
		//{
		//    CreateThreadStaticVariables();

		//    int startIndex = 0;

		//    bytes[startIndex++] = (byte)Transport;
		//    GetBytes(ConnectionId, bytes, ref startIndex);
		//    GetBytes(LocalEndPoint, bytes, ref startIndex);
		//    GetBytes(RemoteEndPoint, bytes, ref startIndex);
		//    GetBytes(userData, bytes, ref startIndex);

		//    HexEncoding.GetLowerHexChars(bytes, 0, startIndex, chars, 0);

		//    return new ArraySegment<byte>(chars, 0, startIndex * 2);
		//}

		//public static ConnectionAddresses? Parse(ByteArrayPart encoded, out int userData)
		//{
		//    CreateThreadStaticVariables();

		//    userData = 0;

		//    if (encoded.IsInvalid || encoded.Length <= 0)
		//        return null;

		//    int length = HexEncoding.TryParseHex(encoded.ToArraySegment(), bytes);
		//    if (length <= 0)
		//        return null;

		//    int index = 0;

		//    var transport = (Transports)bytes[index++];

		//    if (transport != Transports.Udp && transport != Transports.Tcp && transport != Transports.Tls
		//        && transport != Transports.Ws && transport != Transports.Wss)
		//        return null;

		//    if (length - index < 4)
		//        return null;
		//    var connectionId = ToInt32(bytes, ref index);

		//    var localEndPoint = Parse(bytes, length, ref index);
		//    if (localEndPoint == null)
		//        return null;

		//    var remoteEndPoint = Parse(bytes, length, ref index);
		//    if (remoteEndPoint == null)
		//        return null;

		//    if (length - index < 4)
		//        return null;
		//    userData = ToInt32(bytes, ref index);

		//    return new ConnectionAddresses(transport, localEndPoint, remoteEndPoint, connectionId);
		//}

		//private static IPEndPoint Parse(byte[] bytes, int length, ref int startIndex)
		//{
		//    if (length - startIndex < 2 + 1 + 4)
		//        return null;

		//    int port = ToUInt16(bytes, ref startIndex);

		//    int addressBytesCount = bytes[startIndex++];

		//    if (addressBytesCount == 4)
		//    {
		//        long address = (uint)ToInt32(bytes, ref startIndex);
		//        return new IPEndPoint(address, port);
		//    }
		//    else
		//    {
		//        if (addressBytesCount + 4 > length - startIndex)
		//            return null;

		//        var addressBytes = new byte[addressBytesCount];
		//        Buffer.BlockCopy(bytes, startIndex, addressBytes, 0, addressBytesCount);
		//        startIndex += addressBytesCount;

		//        int scopeId = ToInt32(bytes, ref startIndex);

		//        var address = new IPAddress(addressBytes, scopeId);
		//        return new IPEndPoint(address, port);
		//    }
		//}

		//private static void GetBytes(IPEndPoint endpoint, byte[] bytes, ref int startIndex)
		//{
		//    bytes[startIndex++] = (byte)(endpoint.Port >> 8);
		//    bytes[startIndex++] = (byte)(endpoint.Port >> 0);

		//    if (endpoint.AddressFamily == AddressFamily.InterNetwork)
		//    {
		//        bytes[startIndex++] = (byte)(0x04);

		//        GetBytes(unchecked((int)endpoint.Address.Address), bytes, ref startIndex);
		//    }
		//    else
		//    {
		//        var addressBytes = endpoint.Address.GetAddressBytes();

		//        bytes[startIndex++] = (byte)(addressBytes.Length);

		//        Buffer.BlockCopy(addressBytes, 0, bytes, startIndex, addressBytes.Length);
		//        startIndex += addressBytes.Length;

		//        GetBytes(unchecked((int)endpoint.Address.ScopeId), bytes, ref startIndex);
		//    }
		//}

		//private static void GetBytes(int value, byte[] bytes, ref int startIndex)
		//{
		//    bytes[startIndex++] = (byte)(value >> 24);
		//    bytes[startIndex++] = (byte)(value >> 16);
		//    bytes[startIndex++] = (byte)(value >> 8);
		//    bytes[startIndex++] = (byte)(value >> 0);
		//}

		//private static int ToInt32(byte[] bytes, ref int startIndex)
		//{
		//    int value = 0;

		//    value |= bytes[startIndex++];
		//    value <<= 8;
		//    value |= bytes[startIndex++];
		//    value <<= 8;
		//    value |= bytes[startIndex++];
		//    value <<= 8;
		//    value |= bytes[startIndex++];

		//    return value;
		//}

		//private static UInt16 ToUInt16(byte[] bytes, ref int startIndex)
		//{
		//    UInt16 value = 0;

		//    value |= bytes[startIndex++];
		//    value <<= 8;
		//    value |= bytes[startIndex++];

		//    return value;
		//}
	}
}

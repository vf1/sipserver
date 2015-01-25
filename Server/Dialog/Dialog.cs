using System;
using System.Threading;
using System.Net;
using Sip.Message;
using Base.Message;
using Sip.Tools;

namespace Sip.Server
{
	public class Dialog
	{
		private readonly Transports transport;
		private readonly IPEndPoint localEndPoint;
		private readonly IPEndPoint remoteEndPoint;

		private readonly byte[] bytes;

		private readonly int marker1;
		private readonly int marker2;
		private readonly int marker3;
		private readonly int marker4;
		private readonly int marker5;
		private readonly int marker6;
		private readonly int markerN;

		private readonly int routeCount;

		private int localCseq;
		private readonly int remoteCseq; // why readonly ?!

		public Dialog(SipMessageReader request, int localTag, ConnectionAddresses connectionAddresses)
		{
			this.transport = connectionAddresses.Transport;
			this.localEndPoint = connectionAddresses.LocalEndPoint;
			this.remoteEndPoint = connectionAddresses.RemoteEndPoint;

			localCseq = 0;
			remoteCseq = request.CSeq.Value;

			int length = Calculate(request, out routeCount) + 8;

			bytes = new byte[length];

			marker1 = bytes.CopyFrom(request.CallId, 0);
			HexEncoding.GetLowerHexChars(localTag, bytes, marker1);
			marker2 = marker1 + 8;
			marker3 = bytes.CopyFrom(request.From.Tag, marker2);
			marker4 = bytes.CopyFrom(request.To.AddrSpec.Value, marker3);
			marker5 = bytes.CopyFrom(request.From.AddrSpec.Value, marker4);
			marker6 = bytes.CopyFrom(request.From.Epid, marker5);
			markerN = bytes.CopyFrom(request.Contact[0].AddrSpec.Value, marker6);

			CopyRecordRoute(request);
		}

		public Dialog(SipMessageReader response, ConnectionAddresses connectionAddresses)
		{
			this.transport = connectionAddresses.Transport;
			this.localEndPoint = connectionAddresses.LocalEndPoint;
			this.remoteEndPoint = connectionAddresses.RemoteEndPoint;

			localCseq = response.CSeq.Value;
			remoteCseq = 0;

			int length = Calculate(response, out routeCount) + response.To.Tag.Length;

			bytes = new byte[length];

			marker1 = bytes.CopyFrom(response.CallId, 0);
			marker2 = bytes.CopyFrom(response.From.Tag, marker1);
			marker3 = bytes.CopyFrom(response.To.Tag, marker2);
			marker4 = bytes.CopyFrom(response.From.AddrSpec.Value, marker3);
			marker5 = bytes.CopyFrom(response.To.AddrSpec.Value, marker4);
			marker6 = bytes.CopyFrom(response.From.Epid, marker5);
			markerN = bytes.CopyFrom(response.Contact[0].AddrSpec.Value, marker6);

			CopyRecordRoute(response);
		}

		private int Calculate(SipMessageReader request, out int routeCount1)
		{
			int length =
				request.CallId.Length +
				// 8 +
				request.From.Tag.Length +
				request.To.AddrSpec.Value.Length +
				request.From.AddrSpec.Value.Length +
				request.Contact[0].AddrSpec.Value.Length +
				request.From.Epid.Length;

			routeCount1 = 0;

			for (int i = 0; i < request.Count.HeaderCount; i++)
				if (request.Headers[i].HeaderName == HeaderNames.RecordRoute)
				{
					length += request.Headers[i].Value.Length + 2;
					routeCount1++;
				}

			return length;
		}

		private void CopyRecordRoute(SipMessageReader request)
		{
			int offset = markerN + routeCount * 2;
			for (int i = 0; i < routeCount; i++)
				if (request.Headers[i].HeaderName == HeaderNames.RecordRoute)
				{
					int itemOffset = offset;
					bytes[markerN + i * 2] = (byte)(itemOffset);
					bytes[markerN + i * 2 + 1] = (byte)(itemOffset >> 8);

					var part = request.Headers[i].Value;
					Buffer.BlockCopy(part.Bytes, part.Begin, bytes, offset, part.Length);

					offset += part.Length;
				}
		}

		public static bool HasValidId(SipMessageReader message)
		{
			return message.CallId.IsValid && message.To.Tag.IsValid && message.From.Tag.IsValid;
		}

		public static int GetIdLength(SipMessageReader message)
		{
			return message.CallId.Length + message.To.Tag.Length + message.From.Tag.Length;
		}

		public static void GenerateId(SipMessageReader message, byte[] bytes)
		{
			int offset = 0;

			message.CallId.BlockCopyTo(bytes, ref offset);

			if (message.IsRequest)
			{
				message.To.Tag.BlockCopyTo(bytes, ref offset);
				message.From.Tag.BlockCopyTo(bytes, ref offset);
			}
			else
			{
				message.From.Tag.BlockCopyTo(bytes, ref offset);
				message.To.Tag.BlockCopyTo(bytes, ref offset);
			}
		}

		public Transports Transport
		{
			get { return transport; }
		}

		public IPEndPoint LocalEndPoint
		{
			get { return localEndPoint; }
		}

		public IPEndPoint RemoteEndPoint
		{
			get { return remoteEndPoint; }
		}

		public ByteArrayPart Id
		{
			get { return new ByteArrayPart() { Bytes = bytes, Begin = 0, End = marker3, }; }
		}

		public ByteArrayPart CallId
		{
			get { return new ByteArrayPart() { Bytes = bytes, Begin = 0, End = marker1, }; }
		}

		public ByteArrayPart LocalTag
		{
			get { return new ByteArrayPart() { Bytes = bytes, Begin = marker1, End = marker2, }; }
		}

		public ByteArrayPart RemoteTag
		{
			get { return new ByteArrayPart() { Bytes = bytes, Begin = marker2, End = marker3, }; }
		}

		public int GetNextLocalCseq()
		{
			return Interlocked.Increment(ref localCseq);
		}

		public int RemoteCseq
		{
			get { return remoteCseq; }
		}

		public ByteArrayPart LocalUri
		{
			get { return new ByteArrayPart() { Bytes = bytes, Begin = marker3, End = marker4, }; }
		}

		public ByteArrayPart RemoteUri
		{
			get { return new ByteArrayPart() { Bytes = bytes, Begin = marker4, End = marker5, }; }
		}

		public ByteArrayPart Epid
		{
			get { return new ByteArrayPart() { Bytes = bytes, Begin = marker5, End = marker6, }; }
		}

		public ByteArrayPart RemoteTarget
		{
			get { return new ByteArrayPart() { Bytes = bytes, Begin = marker6, End = markerN, }; }
		}

		public int RouteCount
		{
			get { return routeCount; }
		}

		public ByteArrayPart GetRoute(int index)
		{
			int offset = markerN + index * 2;

			int begin = (int)bytes[offset + 1] << 8 + bytes[offset];

			int end = (index < routeCount) ?
				(int)bytes[offset + 3] << 8 + bytes[offset + 2] : bytes.Length + 1;

			return new ByteArrayPart() { Bytes = bytes, Begin = begin, End = end, };
		}

		public ConnectionAddresses ConnectionAddresses
		{
			get { return new ConnectionAddresses(transport, localEndPoint, remoteEndPoint, SocketServers.ServerAsyncEventArgs.AnyNewConnectionId); }
		}
	}
}

using System;
using System.Threading;
using Sip.Message;
using Base.Message;
using Sip.Tools;

using System.Net;

namespace Sip.Server
{
	partial class LocationService
	{
		public sealed class Binding
		{
			private readonly byte[] bytes;
			private readonly int marker1;
			private readonly int marker2;
			private readonly int marker3;
			private readonly int marker4;

			private int stopTickCount;
			private int cseq;

			public Binding(SipMessageReader reader, int contactIndex, int expires1, ConnectionAddresses connectionAddresses)
			{
				ConnectionAddresses = connectionAddresses;

				int length =
					reader.Contact[contactIndex].AddrSpec.Value.Length +
					reader.Contact[contactIndex].SipInstance.Length +
					reader.From.Epid.Length +
					reader.CallId.Length;

				bytes = new byte[length + 8 - length % 8];

				marker1 = bytes.CopyFrom(reader.Contact[contactIndex].AddrSpec.Value, 0);
				marker2 = bytes.CopyFrom(reader.Contact[contactIndex].SipInstance, marker1);
				marker3 = bytes.CopyFrom(reader.From.Epid, marker2);
				marker4 = bytes.CopyFrom(reader.CallId, marker3);

				Update(reader.CSeq.Value, expires1);
			}

			public bool IsChanged(SipMessageReader reader, int contactIndex)
			{
				return
					AddrSpec != reader.Contact[contactIndex].AddrSpec.Value ||
					SipInstance != reader.Contact[contactIndex].SipInstance ||
					Epid != reader.From.Epid ||
					CallId != reader.CallId;
			}

			public void Update(int cseq1, int expires1)
			{
				Thread.VolatileWrite(ref stopTickCount, unchecked(Environment.TickCount + expires1 * 1000));

				cseq = cseq1;
			}

			public ByteArrayPart AddrSpec
			{
				get { return new ByteArrayPart() { Bytes = bytes, Begin = 0, End = marker1, }; }
			}

			public ByteArrayPart SipInstance
			{
				get
				{
					return (marker1 < marker2) ?
						new ByteArrayPart() { Bytes = bytes, Begin = marker1, End = marker2, } :
						ByteArrayPart.Invalid;
				}
			}

			public ByteArrayPart Epid
			{
				get { return new ByteArrayPart() { Bytes = bytes, Begin = marker2, End = marker3, }; }
			}

			public ByteArrayPart CallId
			{
				get { return new ByteArrayPart() { Bytes = bytes, Begin = marker3, End = marker4, }; }
			}

			public int Expires
			{
				get
				{
					int expires = unchecked(
						Thread.VolatileRead(ref stopTickCount) - Environment.TickCount) / 1000;

					return (expires < 0) ? 0 : expires;
				}
			}

			public bool IsExpired
			{
				get { return Expires <= 0; }
			}

			public bool IsNewData(ByteArrayPart callId, int cseq1)
			{
				return CallId != callId || cseq < cseq1;
			}

			public ConnectionAddresses ConnectionAddresses
			{
				get;
				private set;
			}
		}
	}
}

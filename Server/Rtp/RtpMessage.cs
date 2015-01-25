using System;

namespace Rtp
{
	class RtpMessage
	{
		public int Version { get; set; }
		public bool Padding { get; set; }
		public bool Extension { get; set; }
		public bool Marker { get; set; }
		public int PayloadType { get; set; }
		public UInt16 SequenceNumber { get; set; }
		public UInt32 Timestamp { get; set; }
		public UInt32 Ssrc { get; set; }
		public UInt32[] CsrcList { get; set; }
		public object Payload { get; set; }

		public UInt64 NtpTimestamp { get; set; }

		public T GetPayload<T>()
		{
			return (T)Payload;
		}

		public int CsrcCount 
		{
			get
			{
				return (CsrcList != null) ? CsrcList.Length : 0;
			}
		}

		public int GetPayloadOffset(int startIndex)
		{
			return startIndex + 12 + (int)CsrcCount * 4;
		}

		public int GetPayloadLength(int startIndex, int length)
		{
			return length - GetPayloadOffset(startIndex);
		}

		public static RtpMessage Parse(byte[] bytes, int startIndex, int length)
		{
			var message = new RtpMessage();

			message.Parse2(bytes, startIndex, length);

			return message;
		}

		protected void Parse2(byte[] bytes, int startIndex, int length)
		{
			Version = (bytes[startIndex] & 0xb0) >> 6;
			if (Version != 2)
				throw new ParseException(@"Version != 2");
			Padding = (bytes[startIndex] & 0x20) != 0;
			Extension = (bytes[startIndex] & 0x10) != 0;
			CsrcList = new UInt32[bytes[startIndex] & 0x0f];

			startIndex++;

			Marker = (bytes[startIndex] & 0x80) != 0;
			PayloadType = bytes[startIndex] & 0x7f;

			startIndex++;

			SequenceNumber = bytes.BigendianToUInt16(ref startIndex);
			Timestamp = bytes.BigendianToUInt32(ref startIndex);
			Ssrc = bytes.BigendianToUInt32(ref startIndex);

			for (int i = 0; i < CsrcCount; i++)
				CsrcList[i] = bytes.BigendianToUInt32(ref startIndex);
		}

		public void GetBytes(byte[] bytes, int startIndex)
		{
			throw new NotImplementedException();
		}

		public static bool IsRtpMessage(byte[] bytes, int startIndex, int length)
		{
			return ((bytes[startIndex] & 0xb0) >> 6) == 2 &&
				bytes[startIndex + 1] != (byte)RtcpMessageType.SenderReport &&
				bytes[startIndex + 1] != (byte)RtcpMessageType.ReceiverReport;
		}
	}
}

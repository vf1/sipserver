using System;
using System.Text;
using System.Collections.Generic;

namespace Rtp
{
	enum RtcpMessageType
	{
		SenderReport = 200,
		ReceiverReport = 201,
		SourceDescription = 202,
		Goodbye = 203,
		ApplicationDefined = 204,
	}

	class RtcpMessage
	{
		protected const int HeaderLength = 4;

		public int Version { get; set; }
		public bool Padding { get; set; }
		public int Count { get; set; }
		public RtcpMessageType MessageType { get; set; }
		public int Length { get; set; }

		public int SubType
		{
			get
			{
				return Count;
			}
			set
			{
				Count = value;
			}
		}

		public SenderReportPacket SenderReport { get; set; }
		public ReceiverReportPacket ReceiverReport { get; set; }
		public SourceDescriptionPacket SourceDescription { get; set; }
		public GoodbyePacket Goodbye { get; set; }
		public ApplicationDefinedPacket ApplicationDefined { get; set; }

		public static List<RtcpMessage> Parse(byte[] bytes, int startIndex, int length)
		{
			return Parse(bytes, ref startIndex, length);
		}

		public static List<RtcpMessage> Parse(byte[] bytes, ref int startIndex, int length)
		{
			List<RtcpMessage> messages = new List<RtcpMessage>();

			while (startIndex < length)
			{
				var message = new RtcpMessage();
				message.Parse2(bytes, ref startIndex, length);

				messages.Add(message);
			}

			return messages;
		}

		protected void Parse2(byte[] bytes, ref int startIndex, int length)
		{
			Version = (bytes[startIndex] & 0xb0) >> 6;
			if (Version != 2)
				throw new ParseException(@"Version != 2");

			Padding = (bytes[startIndex] & 0x20) != 0;
			Count = bytes[startIndex] & 0x1f;

			startIndex++;

			if (Enum.IsDefined(typeof(RtcpMessageType), (int)bytes[startIndex]) == false)
				throw new ParseException(@"Invalid RtcpMessageType");
			MessageType = (RtcpMessageType)bytes[startIndex++];

			Length = (bytes.BigendianToUInt16(ref startIndex) + 1) * 4;
			if (Length > length - startIndex + HeaderLength)
				throw new ParseException(@"Invalid Length");
			int packetDataLength = startIndex + Length - HeaderLength;

			switch (MessageType)
			{
				case RtcpMessageType.SenderReport:
					SenderReport = new SenderReportPacket();
					SenderReport.Parse(bytes, ref startIndex, packetDataLength, Count);
					break;

				case RtcpMessageType.ReceiverReport:
					ReceiverReport = new ReceiverReportPacket();
					ReceiverReport.Parse(bytes, ref startIndex, packetDataLength, Count);
					break;

				case RtcpMessageType.SourceDescription:
					SourceDescription = new SourceDescriptionPacket();
					SourceDescription.Parse(bytes, ref startIndex, packetDataLength, Count);
					break;

				case RtcpMessageType.Goodbye:
					Goodbye = new GoodbyePacket();
					Goodbye.Parse(bytes, ref startIndex, packetDataLength, Count);
					break;

				case RtcpMessageType.ApplicationDefined:
					ApplicationDefined = new ApplicationDefinedPacket();
					ApplicationDefined.Parse(bytes, ref startIndex, packetDataLength, Count);
					break;

				default:
					throw new Exception();
			}
		}

		public void GetBytes(byte[] bytes, int startIndex)
		{
			throw new NotImplementedException();
		}

		public static bool IsRtcpMessage(byte[] bytes, int startIndex, int length)
		{
			return ((bytes[startIndex] & 0xb0) >> 6) == 2 &&
				(bytes[startIndex + 1] == (byte)RtcpMessageType.SenderReport || 
					bytes[startIndex + 1] == (byte)RtcpMessageType.ReceiverReport);
		}
	}

	class SenderReportPacket
	{
		public UInt32 Ssrc { get; set; }
		public UInt64 NtpTimestamp { get; set; }
		public UInt32 RtpTimestamp { get; set; }
		public UInt32 PacketCount { get; set; }
		public UInt32 OctetCount { get; set; }
		public ReportBlockPart[] ReportBlocks { get; set; }

		public void Parse(byte[] bytes, ref int startIndex, int length, int reportCount)
		{
			Ssrc = bytes.BigendianToUInt32(ref startIndex);

			NtpTimestamp = (UInt64)bytes.BigendianToUInt32(ref startIndex) << 32
				| bytes.BigendianToUInt32(ref startIndex);

			RtpTimestamp = bytes.BigendianToUInt32(ref startIndex);

			PacketCount = bytes.BigendianToUInt32(ref startIndex);
			OctetCount = bytes.BigendianToUInt32(ref startIndex);

			ReportBlocks = new ReportBlockPart[reportCount];
			for (int i = 0; i < ReportBlocks.Length; i++)
			{
				ReportBlocks[i] = new ReportBlockPart();
				ReportBlocks[i].Parse(bytes, ref startIndex, length);
			}
		}
	}

	/// <summary>
	/// Not tested!
	/// </summary>
	class ReceiverReportPacket
	{
		public UInt32 Ssrc { get; set; }
		public ReportBlockPart[] ReportBlocks { get; set; }

		public void Parse(byte[] bytes, ref int startIndex, int length, int reportCount)
		{
			Ssrc = bytes.BigendianToUInt32(ref startIndex);

			ReportBlocks = new ReportBlockPart[reportCount];
			for (int i = 0; i < ReportBlocks.Length; i++)
			{
				ReportBlocks[i] = new ReportBlockPart();
				ReportBlocks[i].Parse(bytes, ref startIndex, length);
			}
		}
	}

	class ReportBlockPart
	{
		public UInt32 SsrcN { get; set; }
		public Byte FractionLost { get; set; }
		public UInt32 PacketsLost { get; set; }
		public UInt32 HighestSequence { get; set; }
		public UInt32 InterarrivalJitter { get; set; }
		public UInt32 LastSrTimestamp { get; set; }
		public UInt32 DelaySinceLastSr { get; set; }

		public void Parse(byte[] bytes, ref int startIndex, int length)
		{
			SsrcN = bytes.BigendianToUInt32(ref startIndex);
			FractionLost = bytes[startIndex++];
			PacketsLost = bytes.BigendianToUInt24(ref startIndex);
			HighestSequence = bytes.BigendianToUInt32(ref startIndex);
			InterarrivalJitter = bytes.BigendianToUInt32(ref startIndex);
			LastSrTimestamp = bytes.BigendianToUInt32(ref startIndex);
			DelaySinceLastSr = bytes.BigendianToUInt32(ref startIndex);
		}
	}

	class SourceDescriptionPacket
	{
		public SourceDescriptionPacketChunk[] Chunks { get; set; }

		public void Parse(byte[] bytes, ref int startIndex, int length, int count)
		{
			Chunks = new SourceDescriptionPacketChunk[count];

			for (int i = 0; i < Chunks.Length; i++)
			{
				UInt32 ssrc = bytes.BigendianToUInt32(ref startIndex);

				if (Enum.IsDefined(typeof(SourceDescriptionPacketChunk.ItemType), (int)bytes[startIndex]) == false)
					throw new ParseException(@"Invalid SourceDescriptionPacketChunk.ItemType value");
				var itemType = (SourceDescriptionPacketChunk.ItemType)bytes[startIndex++];

				string value = null;
				int prefixLength = 0;
				if (itemType == SourceDescriptionPacketChunk.ItemType.Priv)
					value = RtcpString.Decode(bytes, ref startIndex, length, out prefixLength);
				else
					value = RtcpString.Decode(bytes, ref startIndex, length);

				Chunks[i] = new SourceDescriptionPacketChunk()
				{
					Ssrc = ssrc,
					Type = itemType,
					Value = value,
					PrefixLength = prefixLength,
				};

				// Each chunk starts on a 32-bit boundary.
				startIndex += (startIndex % 4 > 0) ? 4 - startIndex % 4 : 0;
			}
		}
	}

	class SourceDescriptionPacketChunk
	{
		public enum ItemType
		{
			Cname = 1,
			Name = 2,
			Email = 3,
			Phone = 4,
			Loc = 5,
			Tool = 6,
			Note = 7,
			Priv = 8,
		}

		public UInt32 Ssrc { get; set; }
		public ItemType Type { get; set; }
		public string Value { get; set; }
		public int PrefixLength { get; set; }
	}

	class GoodbyePacket
	{
		public UInt32[] Ssrcs { get; set; }
		public string Reason { get; set; }

		public void Parse(byte[] bytes, ref int startIndex, int length, int count)
		{
			Ssrcs = new UInt32[count];
			for (int i = 0; i < Ssrcs.Length; i++)
				Ssrcs[i] = bytes.BigendianToUInt32(ref startIndex);

			if (startIndex < length)
				Reason = RtcpString.Decode(bytes, ref startIndex, length);
		}
	}

	/// <summary>
	/// Not tested!
	/// </summary>
	class ApplicationDefinedPacket
	{
		public void Parse(byte[] bytes, ref int startIndex, int length, int subtype)
		{
			startIndex += length - startIndex;
		}
	}

	static class RtcpString
	{
		public static string Decode(byte[] bytes, ref int startIndex, int length, out int prefixLength)
		{
			int stringLength = bytes[startIndex++];
			prefixLength = bytes[startIndex++];

			return RtcpString.Decode(stringLength, bytes, ref startIndex, length);
		}

		public static string Decode(byte[] bytes, ref int startIndex, int length)
		{
			return RtcpString.Decode(bytes[startIndex++], bytes, ref startIndex, length);
		}

		private static string Decode(int stringLength, byte[] bytes, ref int startIndex, int length)
		{
			if (stringLength > length - startIndex)
				throw new ParseException(@"Invalid RtcpString");

			var ascii = new ASCIIEncoding();

			var result = ascii.GetString(bytes, startIndex, stringLength);
			startIndex += stringLength;

			return result;
		}
	}
}

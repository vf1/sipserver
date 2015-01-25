using Rtp;
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TestRtp
{
	[TestFixture]
	public class RtcpMessageTest
	{
		// SR, SDES
		private byte[] rawMessage1 = new byte[]
		{
			0x81, 0xC8, 0x00, 0x0C, 0x00, 0x01, 0x50, 0x61,
			0xCE, 0xA3, 0xF3, 0x34, 0x4A, 0xE0, 0x4C, 0x05,
			0x06, 0xE0, 0x44, 0x47, 0x00, 0x00, 0x00, 0x23,
			0x00, 0x00, 0x08, 0x35, 0xEA, 0x70, 0x89, 0x6C,
			0x13, 0x00, 0x00, 0x03, 0x00, 0x00, 0xBD, 0x47,
			0x00, 0x00, 0x00, 0x30, 0x01, 0x02, 0x03, 0x04,
			0x01, 0x02, 0x03, 0x04, 0x81, 0xCA, 0x00, 0x06,
			0x00, 0x01, 0x50, 0x61, 0x01, 0x11, 0x75, 0x73,
			0x65, 0x72, 0x40, 0x67, 0x69, 0x7A, 0x6D, 0x6F,
			0x70, 0x72, 0x6F, 0x6A, 0x65, 0x63, 0x74, 0x00,
		};

		// SR, SDES, BYE
		private byte[] rawMessage2 = new byte[]
		{
			0x81, 0xC8, 0x00, 0x0C, 0x00, 0x01, 0x50, 0x61,
			0xCE, 0xA3, 0xF3, 0x72, 0x2B, 0x21, 0xD5, 0x3C,
			0x83, 0x77, 0xAF, 0xC3, 0x00, 0x00, 0x00, 0x2E,
			0x00, 0x00, 0x08, 0xB9, 0x3B, 0x5B, 0x22, 0x9E,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x83, 0x62,
			0x0F, 0xFD, 0x25, 0x3F, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x81, 0xCA, 0x00, 0x06,
			0x00, 0x01, 0x50, 0x61, 0x01, 0x11, 0x75, 0x73,
			0x65, 0x72, 0x40, 0x67, 0x69, 0x7A, 0x6D, 0x6F,
			0x70, 0x72, 0x6F, 0x6A, 0x65, 0x63, 0x74, 0x00,
			0x81, 0xCB, 0x00, 0x01, 0x00, 0x01, 0x50, 0x61,
		};

		[Test]
		public void ParseTest1()
		{
			int startIndex = 0;
			List<RtcpMessage> messages = RtcpMessage.Parse(rawMessage1, ref startIndex, rawMessage1.Length);

			Assert.AreEqual(rawMessage1.Length, startIndex);
			Assert.AreEqual(false, messages[0].Padding);
			Assert.AreEqual(false, messages[1].Padding);

			Assert.AreEqual(2, messages.Count);

			Assert.AreEqual(RtcpMessageType.SenderReport, messages[0].MessageType);
			Assert.AreEqual(RtcpMessageType.SourceDescription, messages[1].MessageType);

			Assert.AreEqual((UInt32)0x15061, messages[0].SenderReport.Ssrc);
			Assert.AreEqual((UInt64)0xCEA3F3344AE04C05, messages[0].SenderReport.NtpTimestamp);
			Assert.AreEqual((UInt32)0x6E04447, messages[0].SenderReport.RtpTimestamp);
			Assert.AreEqual((UInt32)0x00000023, messages[0].SenderReport.PacketCount);
			Assert.AreEqual((UInt32)0x00000835, messages[0].SenderReport.OctetCount);
			
			Assert.AreEqual(1, messages[0].SenderReport.ReportBlocks.Length);
			Assert.AreEqual((UInt32)0xEA70896C, messages[0].SenderReport.ReportBlocks[0].SsrcN);
			Assert.AreEqual((Byte)0x13, messages[0].SenderReport.ReportBlocks[0].FractionLost);
			Assert.AreEqual((UInt32)0x3, messages[0].SenderReport.ReportBlocks[0].PacketsLost);
			Assert.AreEqual((UInt32)0xBD47, messages[0].SenderReport.ReportBlocks[0].HighestSequence);
			Assert.AreEqual((UInt32)0x30, messages[0].SenderReport.ReportBlocks[0].InterarrivalJitter);
			Assert.AreEqual((UInt32)0x01020304, messages[0].SenderReport.ReportBlocks[0].LastSrTimestamp);
			Assert.AreEqual((UInt32)0x01020304, messages[0].SenderReport.ReportBlocks[0].DelaySinceLastSr);

			Assert.AreEqual(1, messages[1].SourceDescription.Chunks.Length);
			Assert.AreEqual((UInt32)0x15061, messages[1].SourceDescription.Chunks[0].Ssrc);
			Assert.AreEqual(SourceDescriptionPacketChunk.ItemType.Cname, messages[1].SourceDescription.Chunks[0].Type);
			Assert.AreEqual(@"user@gizmoproject", messages[1].SourceDescription.Chunks[0].Value);
			Assert.AreEqual(0, messages[1].SourceDescription.Chunks[0].PrefixLength);
		}

		[Test]
		public void ParseTest2()
		{
			int startIndex = 0;
			List<RtcpMessage> messages = RtcpMessage.Parse(rawMessage2, ref startIndex, rawMessage2.Length);

			Assert.AreEqual(rawMessage2.Length, startIndex);
			Assert.AreEqual(3, messages.Count);
			Assert.AreEqual(false, messages[0].Padding);
			Assert.AreEqual(false, messages[1].Padding);
			Assert.AreEqual(false, messages[2].Padding);

			Assert.AreEqual(RtcpMessageType.SenderReport, messages[0].MessageType);
			Assert.AreEqual(RtcpMessageType.SourceDescription, messages[1].MessageType);
			Assert.AreEqual(RtcpMessageType.Goodbye, messages[2].MessageType);

			Assert.AreEqual((UInt32)0x15061, messages[0].SenderReport.Ssrc);
			Assert.AreEqual((UInt64)0xCEA3F3722B21D53C, messages[0].SenderReport.NtpTimestamp);
			Assert.AreEqual((UInt32)0x8377AFC3, messages[0].SenderReport.RtpTimestamp);
			Assert.AreEqual((UInt32)0x0000002E, messages[0].SenderReport.PacketCount);
			Assert.AreEqual((UInt32)0x000008B9, messages[0].SenderReport.OctetCount);

			Assert.AreEqual(1, messages[0].SenderReport.ReportBlocks.Length);
			Assert.AreEqual((UInt32)0x3B5B229E, messages[0].SenderReport.ReportBlocks[0].SsrcN);
			Assert.AreEqual((Byte)0x0, messages[0].SenderReport.ReportBlocks[0].FractionLost);
			Assert.AreEqual((UInt32)0x0, messages[0].SenderReport.ReportBlocks[0].PacketsLost);
			Assert.AreEqual((UInt32)0x8362, messages[0].SenderReport.ReportBlocks[0].HighestSequence);
			Assert.AreEqual((UInt32)0xFFD253F, messages[0].SenderReport.ReportBlocks[0].InterarrivalJitter);
			Assert.AreEqual((UInt32)0x0, messages[0].SenderReport.ReportBlocks[0].LastSrTimestamp);
			Assert.AreEqual((UInt32)0x0, messages[0].SenderReport.ReportBlocks[0].DelaySinceLastSr);

			Assert.AreEqual(1, messages[1].SourceDescription.Chunks.Length);
			Assert.AreEqual((UInt32)0x15061, messages[1].SourceDescription.Chunks[0].Ssrc);
			Assert.AreEqual(SourceDescriptionPacketChunk.ItemType.Cname, messages[1].SourceDescription.Chunks[0].Type);
			Assert.AreEqual(@"user@gizmoproject", messages[1].SourceDescription.Chunks[0].Value);
			Assert.AreEqual(0, messages[1].SourceDescription.Chunks[0].PrefixLength);

			Assert.AreEqual(1, messages[2].Goodbye.Ssrcs.Length);
			Assert.AreEqual((UInt32)0x15061, messages[2].Goodbye.Ssrcs[0]);
			Assert.IsNull(messages[2].Goodbye.Reason);
		}

		[Test]
		public void IsRtcpMessageTest()
		{
			byte[] bytes1 = new byte[]
			{
				0xff, 0xff, 0xff, 0xff,
				0xb2, 0xEA, 0x46, 0x5C,
			};

			byte[] bytes2 = new byte[]
			{
				0xb2, 0xC8, 0x46, 0x5C,
			};

			byte[] bytes3 = new byte[]
			{
				0xb2, 0xC9, 0x46, 0x5C,
			};

			byte[] bytes4 = new byte[]
			{
				0x02, 0xEA, 0x46, 0x5C,
			};

			Assert.IsFalse(RtcpMessage.IsRtcpMessage(bytes1, 4, bytes1.Length));
			Assert.IsTrue(RtcpMessage.IsRtcpMessage(bytes2, 0, bytes2.Length));
			Assert.IsTrue(RtcpMessage.IsRtcpMessage(bytes3, 0, bytes3.Length));
			Assert.IsFalse(RtcpMessage.IsRtcpMessage(bytes4, 0, bytes4.Length));
		}
	}
}

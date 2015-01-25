using Rtp;
using System;
using NUnit.Framework;

namespace TestRtp
{
	[TestFixture]
	public class RtpMessageTest
	{
		[Test]
		public void ParseTest()
		{
			byte[] bytes1 = new byte[]
			{
				0xff, 0xff, 0xff, 0xff,
				0xb2, 0xEA, 0x46, 0x5C,
				0x06, 0xE7, 0x95, 0xA7,
				0x00, 0x01, 0x50, 0x61,
				0x00, 0x00, 0x00, 0x01,	// CSRC1
				0x00, 0x00, 0x00, 0x02, // CSRC2
				0x01, 0x0A, 0x03, 0xC0,
			};
			int startIndex1 = 4;
			int length = bytes1.Length;

			var message1 = RtpMessage.Parse(bytes1, startIndex1, length);

			Assert.AreEqual(message1.Version, 2, "Rtp.Version");
			Assert.AreEqual(message1.Padding, true, "Rtp.Padding");
			Assert.AreEqual(message1.Extension, true, "Rtp.Extension");
			Assert.AreEqual(message1.CsrcCount, 2, "Rtp.CsrcCount");
			Assert.AreEqual(message1.Marker, true, "Rtp.Marker");
			Assert.AreEqual(message1.PayloadType, 106, "Rtp.PayloadType");
			Assert.AreEqual(message1.SequenceNumber, 0x465c, "Rtp.SequenceNumber");
			Assert.AreEqual(message1.Timestamp, 0x006E795A7u, "Rtp.Timestamp");
			Assert.AreEqual(message1.Ssrc, 0x00015061u, "Rtp.Ssrc");
			Assert.AreEqual(message1.CsrcList[0], 0x00000001u, "CsrcList[0]");
			Assert.AreEqual(message1.CsrcList[1], 0x00000002u, "CsrcList[1]");
			Assert.AreEqual(message1.GetPayloadLength(startIndex1, bytes1.Length), 4, "Rtp.PayloadLength");
		}

		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void GetBytesTest()
		{
			RtpMessage target = new RtpMessage(); // TODO: Initialize to an appropriate value
			byte[] bytes = null; // TODO: Initialize to an appropriate value
			int startIndex = 0; // TODO: Initialize to an appropriate value
			target.GetBytes(bytes, startIndex);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		[Test]
		public void IsRtpMessageTest()
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

			Assert.IsTrue(RtpMessage.IsRtpMessage(bytes1, 4, bytes1.Length));
			Assert.IsFalse(RtpMessage.IsRtpMessage(bytes2, 0, bytes2.Length));
			Assert.IsFalse(RtpMessage.IsRtpMessage(bytes3, 0, bytes3.Length));
			Assert.IsFalse(RtpMessage.IsRtpMessage(bytes4, 0, bytes4.Length));
		}
	}
}

using Rtp;
using System;
using NUnit.Framework;

namespace TestRtp
{
	[TestFixture]
	public class SsrcTest
	{
		[Test]
		public void GetNtpTimestampTest()
		{
			Ssrc ssrc = new Ssrc(new int[] { 0 }, new int[] { 1 });

			ssrc.SetBaseTimestamp(0x0000001000000000UL, 0xfffffff0);

			// message's rtp timestamp >= base rtp timestamp
			Assert.AreEqual(0x0000001000000000UL, ssrc.GetNtpTimestamp(new RtpMessage() { Timestamp = 0xfffffff0, }, 1));
			Assert.AreEqual(0x0000001100000000UL, ssrc.GetNtpTimestamp(new RtpMessage() { Timestamp = 0xfffffff1, }, 1));
			Assert.AreEqual(0x0000001f00000000UL, ssrc.GetNtpTimestamp(new RtpMessage() { Timestamp = 0xffffffff, }, 1));
			Assert.AreEqual(0x0000002000000000UL, ssrc.GetNtpTimestamp(new RtpMessage() { Timestamp = 0x00000000, }, 1));
			Assert.AreEqual(0x0000002100000000UL, ssrc.GetNtpTimestamp(new RtpMessage() { Timestamp = 0x00000001, }, 1));

			// message's rtp timestamp < base rtp timestamp
			Assert.AreEqual(0x0000000f00000000UL, ssrc.GetNtpTimestamp(new RtpMessage() { Timestamp = 0xffffffef, }, 1));
			Assert.AreEqual(0x0000000000000000UL, ssrc.GetNtpTimestamp(new RtpMessage() { Timestamp = 0xffffffe0, }, 1));
		}
	}
}

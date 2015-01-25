using System;
using Rtp;
using Rtp.Payload;
using NUnit.Framework;

namespace TestRtp
{
	[TestFixture]
	public class DtmfEventMessageTest
	{
		[Test]
		public void ParseTest1()
		{
			byte[] bytes1 = new byte[] { 0xff, 0x01, 0x8A, 0x03, 0xC0, };

			var messages1 = DtmfEventMessage.Parse(bytes1, 1, bytes1.Length);

			Assert.AreEqual(1, messages1.Length);
			Assert.AreEqual(1, messages1[0].Event, "Dtmf.Event");
			Assert.AreEqual(true, messages1[0].End, "Dtmf.End");
			Assert.AreEqual(0x0a, messages1[0].Volume, "Dtmf.Volume");
			Assert.AreEqual(0x03c0, messages1[0].Duration, "Dtmf.Duration");
		}

		[Test]
		public void ParseTest2()
		{
			byte[] bytes = new byte[] { 0xff, 0x01, 0x8A, 0x03, 0xC0, 0x02, 0x0B, 0x04, 0xC1, };

			var messages = DtmfEventMessage.Parse(bytes, 1, bytes.Length);

			Assert.AreEqual(2, messages.Length, "messages.Length");

			Assert.AreEqual(1, messages[0].Event, "Dtmf.Event");
			Assert.AreEqual(true, messages[0].End, "Dtmf.End");
			Assert.AreEqual(0x0a, messages[0].Volume, "Dtmf.Volume");
			Assert.AreEqual(0x03c0, messages[0].Duration, "Dtmf.Duration");

			Assert.AreEqual(2, messages[1].Event, "Dtmf.Event");
			Assert.AreEqual(false, messages[1].End, "Dtmf.End");
			Assert.AreEqual(0x0b, messages[1].Volume, "Dtmf.Volume");
			Assert.AreEqual(0x04c1, messages[1].Duration, "Dtmf.Duration");
		}

		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void GetBytesTest()
		{
			DtmfEventMessage target = new DtmfEventMessage(); // TODO: Initialize to an appropriate value
			byte[] bytes = null; // TODO: Initialize to an appropriate value
			int startIndex = 0; // TODO: Initialize to an appropriate value
			target.GetBytes(bytes, startIndex);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}
	}
}

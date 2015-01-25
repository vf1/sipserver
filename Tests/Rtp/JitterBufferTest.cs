using System;
using Rtp;
using NUnit.Framework;

namespace TestRtp
{
	[TestFixture]
	public class JitterBufferTest
	{
		RtpMessage message1 = new RtpMessage() { SequenceNumber = 1, };
		RtpMessage message2 = new RtpMessage() { SequenceNumber = 2, };
		RtpMessage message3 = new RtpMessage() { SequenceNumber = 3, };

		[Test]
		public void JitterBufferTest1()
		{
			JitterBuffer buffer = new JitterBuffer(null);

			buffer.Insert(message1);
			buffer.Insert(message1);
			buffer.Insert(message2);
			buffer.Insert(message2);
			buffer.Insert(message3);
			buffer.Insert(message3);

			Assert.AreEqual(3, buffer.Count);
			Assert.AreEqual((UInt16)1, buffer.DequeueAvailable().SequenceNumber);
			Assert.AreEqual((UInt16)2, buffer.DequeueAvailable().SequenceNumber);
			Assert.AreEqual((UInt16)3, buffer.DequeueAvailable().SequenceNumber);
			Assert.IsNull(buffer.DequeueAvailable());
		}

		[Test]
		public void JitterBufferTest2()
		{
			JitterBuffer buffer = new JitterBuffer(null);

			buffer.Insert(message3);
			buffer.Insert(message3);
			buffer.Insert(message1);
			buffer.Insert(message1);
			buffer.Insert(message2);
			buffer.Insert(message2);

			Assert.AreEqual(3, buffer.Count);
			Assert.AreEqual((UInt16)1, buffer.DequeueAvailable().SequenceNumber);
			Assert.AreEqual((UInt16)2, buffer.DequeueAvailable().SequenceNumber);
			Assert.AreEqual((UInt16)3, buffer.DequeueAvailable().SequenceNumber);
			Assert.IsNull(buffer.DequeueAvailable());
		}
	}
}

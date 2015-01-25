using System;
using System.NtpTimestamp;
using NUnit.Framework;

namespace TestRtp
{
	[TestFixture]
	public class NtpTimestampTest
	{
		[Test]
		public void NtpTimestampTest1()
		{
			UInt64 timestamp1 = 0xCEA3F33E5D0D5A5BL;

			DateTime actual1 = NtpTimestamp.NtpTimestampToUtcDateTime(timestamp1);
			DateTime expected1 = new DateTime(0x08cc3026efdea9a1L); // {10.11.2009 14:12:14.3634849}

			Assert.AreEqual(expected1, actual1);


			UInt64 timestamp2 = actual1.ToNtpTimestamp();

			Assert.AreEqual(0xcea3f33e5d0d58ae, timestamp2);


			UInt64 timestamp3 = (new DateTime(1900, 1, 1)).ToNtpTimestamp();
			UInt64 actual3 = 0;

			Assert.AreEqual(actual3, timestamp3);
		}
	}
}

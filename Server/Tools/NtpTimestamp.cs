using System;

namespace System.NtpTimestamp
{
	static class NtpTimestamp
	{
		public const UInt64 TicksPerSecond = 10000000;
		public readonly static DateTime NtpZeroDateTime = new DateTime(1900, 1, 1);

		public static UInt64 ToNtpTimestamp(this DateTime dateTime)
		{
			UInt64 ticks = (UInt64)(dateTime - NtpZeroDateTime).Ticks;

			UInt64 seconds = ticks / TicksPerSecond;
			UInt64 fractions = ((ticks % TicksPerSecond) << 32) / TicksPerSecond;

			return (seconds << 32) | fractions;
		}

		public static DateTime NtpTimestampToUtcDateTime(this UInt64 timestamp)
		{
			UInt64 seconds = timestamp >> 32;
			UInt64 fractions = timestamp & 0xffffffffL;

			UInt64 ticks = seconds * TicksPerSecond + ((fractions * TicksPerSecond) >> 32);

			return NtpZeroDateTime.AddTicks((Int64)ticks);
		}

		public static UInt64 MillisecondsToNtpTimestamp(this Int32 milliseconds)
		{
#if DEBUG
			if (milliseconds < 0)
				throw new Exception();
#endif

			UInt64 seconds = (UInt64)milliseconds / 1000;
			UInt64 fractions = (((UInt64)milliseconds % 1000) << 32) / 1000;

			return (seconds << 32) | fractions;
		}
	}
}

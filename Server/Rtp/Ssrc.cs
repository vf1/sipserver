using System;
using System.Collections.Generic;
using System.NtpTimestamp;

namespace Rtp
{
	class Ssrc
	{
		private UInt64 baseNtpTimestamp;
		private UInt32 baseRtpTimestamp;
		private Int32 baseTickCount;

		public Ssrc(int[] payloadTypes, int[] payloadRates)
		{
			JitterBuffers = new JitterBuffer[128];

			for (int i = 0; i < payloadTypes.Length; i++)
				JitterBuffers[payloadTypes[i]] = new JitterBuffer(GetNtpTimestamp)
				{
					Rate = payloadRates[i],
				};
		}

		public UInt32 SsrcId 
		{ 
			get; 
			set; 
		}

		public JitterBuffer[] JitterBuffers 
		{ 
			get; 
			private set; 
		}

		public void SetBaseTimestamp(UInt64 ntp, UInt32 rtp)
		{
			baseNtpTimestamp = ntp;
			baseRtpTimestamp = rtp;
			baseTickCount = Environment.TickCount;
		}

		public UInt64 GetCurrentNtpTimestamp(int millisecondsOffset)
		{
			int milliseconds = Environment.TickCount - baseTickCount + millisecondsOffset;

			return (milliseconds > 0) ?
				baseNtpTimestamp + milliseconds.MillisecondsToNtpTimestamp() :
				baseNtpTimestamp - (-milliseconds).MillisecondsToNtpTimestamp();
		}

		public UInt64 GetNtpTimestamp(RtpMessage message)
		{
			return GetNtpTimestamp(message, JitterBuffers[message.PayloadType].Rate);
		}

		public UInt64 GetNtpTimestamp(RtpMessage message, int rate)
		{
			if (rate != 0)
			{
				UInt32 delta1 = message.Timestamp - baseRtpTimestamp;
				UInt32 delta2 = baseRtpTimestamp - message.Timestamp;

				if (delta1 <= delta2)
					return baseNtpTimestamp +
						((UInt64)delta1 << 32) / (UInt64)rate;
				else
					return baseNtpTimestamp -
						((UInt64)delta2 << 32) / (UInt64)rate;
			}

			return 0;
		}

		public void InsertMessage(RtpMessage message)
		{
			if (JitterBuffers[message.PayloadType] != null)
				JitterBuffers[message.PayloadType].Insert(message);
		}
	}
}

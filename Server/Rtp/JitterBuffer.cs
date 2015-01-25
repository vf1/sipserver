using System;
using System.Collections.Generic;

namespace Rtp
{
	class JitterBuffer
	{
		private List<RtpMessage> messages;
		private int dequeuedSequenceNumber;
		private CalculateNtpTimestampDelegate calculateNtpTimestampDelegate;

		public delegate UInt64 CalculateNtpTimestampDelegate(RtpMessage message, int rate);

		public JitterBuffer(CalculateNtpTimestampDelegate calculateNtpTimestampDelegate1)
		{
			messages = new List<RtpMessage>();
			dequeuedSequenceNumber = -1;
			calculateNtpTimestampDelegate = calculateNtpTimestampDelegate1;
		}

		public int Count
		{
			get
			{
				return messages.Count;
			}
		}

		public int Rate
		{
			get;
			set;
		}

		public void Insert(RtpMessage message)
		{
			if (message.SequenceNumber > dequeuedSequenceNumber)
			{
				int index = messages.FindIndex(
					(oldMessage) => { return message.SequenceNumber <= oldMessage.SequenceNumber; });

				if (index < 0)
				{
					messages.Add(message);
				}
				else
				{
					if (messages[index].SequenceNumber != message.SequenceNumber)
						messages.Insert(index, message);
				}
			}
		}

		public RtpMessage DequeueAvailable()
		{
			return DequeueAvailable(UInt64.MaxValue);
		}

		public bool DequeueAvailable(UInt64 maxNtpTimestamp, out RtpMessage message)
		{
			message = DequeueAvailable(maxNtpTimestamp);

			return message != null;
		}

		public RtpMessage DequeueAvailable(UInt64 maxNtpTimestamp)
		{
			if (messages.Count > 0)
			{
				var message = messages[0];

				if (calculateNtpTimestampDelegate != null)
					message.NtpTimestamp = calculateNtpTimestampDelegate(message, Rate);

				if (message.NtpTimestamp <= maxNtpTimestamp)
				{
					messages.RemoveAt(0);
					dequeuedSequenceNumber = message.SequenceNumber;

					return message;
				}
			}

			return null;
		}
	}
}

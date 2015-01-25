using System;
using System.Collections.Generic;

namespace Rtp.Payload
{
	class DtmfEventMessage
	{
		//  0                   1                   2                   3
		//  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		// |     event     |E|R| volume    |          duration             |
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

		public const int Length = 4;

		public byte Event { get; set; }
		public bool End { get; set; }
		public int Volume { get; set; }
		public int Duration { get; set; }

		public static DtmfEventMessage[] Parse(byte[] bytes, int startIndex, int length)
		{
			if ((length - startIndex) % Length != 0)
				throw new ParseException(@"DtmfEventMessage: Invalid message length");
			int count = (length - startIndex) / Length;

			var messages = new DtmfEventMessage[count];

			for (int i = 0; i < count; i++)
			{
				messages[i] = new DtmfEventMessage();
				messages[i].Parse2(bytes, startIndex, length);

				startIndex += Length;
			}

			return messages;
		}

		protected void Parse2(byte[] bytes, int startIndex, int length)
		{
			Event = bytes[startIndex++];

			End = (bytes[startIndex] & 0x80) != 0;
			Volume = bytes[startIndex] & 0x3f;
			startIndex++;

			Duration = bytes.BigendianToUInt16(ref startIndex);
		}

		public void GetBytes(byte[] bytes, int startIndex)
		{
			throw new NotImplementedException();
		}
	}
}

using System;

namespace System
{
	static class Bigendian
	{
		public static UInt16 BigendianToUInt16(this byte[] bytes, int startIndex)
		{
			UInt16 result = 0;

			result |= bytes[startIndex];
			result <<= 8;
			result |= bytes[startIndex + 1];

			return result;
		}

		public static UInt32 BigendianToUInt32(this byte[] bytes, int startIndex)
		{
			UInt32 result = 0;

			result |= bytes[startIndex];
			result <<= 8;
			result |= bytes[startIndex + 1];
			result <<= 8;
			result |= bytes[startIndex + 2];
			result <<= 8;
			result |= bytes[startIndex + 3];

			return result;
		}

		public static UInt32 BigendianToUInt24(this byte[] bytes, int startIndex)
		{
			UInt32 result = 0;

			result |= bytes[startIndex];
			result <<= 8;
			result |= bytes[startIndex + 1];
			result <<= 8;
			result |= bytes[startIndex + 2];

			return result;
		}

		public static UInt16 BigendianToUInt16(this byte[] bytes, ref int startIndex)
		{
			UInt16 result = BigendianToUInt16(bytes, startIndex);
			startIndex += sizeof(UInt16);
			return result;
		}

		public static UInt32 BigendianToUInt32(this byte[] bytes, ref int startIndex)
		{
			UInt32 result = BigendianToUInt32(bytes, startIndex);
			startIndex += sizeof(UInt32);
			return result;
		}

		public static UInt32 BigendianToUInt24(this byte[] bytes, ref int startIndex)
		{
			UInt32 result = BigendianToUInt24(bytes, startIndex);
			startIndex += sizeof(UInt32) - sizeof(Byte);
			return result;
		}

		private static byte[] Correct(byte[] data)
		{
			if (BitConverter.IsLittleEndian)
				Array.Reverse(data);
			return data;
		}

		public static byte[] GetBigendianBytes(this UInt32 value)
		{
			return Correct(BitConverter.GetBytes(value));
		}

		public static byte[] GetBigendianBytes(this UInt16 value)
		{
			return Correct(BitConverter.GetBytes(value));
		}
	}
}

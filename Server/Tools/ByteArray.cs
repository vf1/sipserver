using System;
using Base.Message;
using Sip.Message;

namespace Sip.Tools
{
	public static class ByteArray
	{
		public static int CopyFrom(this byte[] bytes, ByteArrayPart part, int offset)
		{
			int length = part.Length;

			if (length > 0)
				Buffer.BlockCopy(part.Bytes, part.Begin, bytes, offset, length);

			return offset + length;
		}

		public static int CopyFrom(this byte[] bytes, int value, int offset)
		{
			bytes[offset++] = (byte)(value >> 24);
			bytes[offset++] = (byte)(value >> 16);
			bytes[offset++] = (byte)(value >> 08);
			bytes[offset++] = (byte)(value >> 00);

			return offset;
		}

		public static bool IsEqualValue(this byte[] x, byte[] y)
		{
			if (x.Length != y.Length)
				return false;

			for (int i = 0; i < x.Length; i++)
				if (x[i] != y[i])
					return false;

			return true;
		}

		public static int GetValueHashCode(this byte[] bytes)
		{
			int value = 0;
			int maxOffset = bytes.Length - 1;

			if (maxOffset >= 0)
			{
				for (int i = 0; i <= 3; i++)
				{
					value <<= 8;
					value |= bytes[maxOffset * i / 3];
				}
			}

			return value;
		}
	}
}

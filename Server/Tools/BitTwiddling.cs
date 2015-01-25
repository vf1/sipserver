using System;

namespace System
{
	/// <summary>
	/// http://graphics.stanford.edu/~seander/bithacks.html
	/// </summary>
	class BitTwiddling
	{
		private static readonly int[] multiplyDeBruijnBitPosition = new int[32] { 0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30, 8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31 };

		/// <summary>
		/// Find the log base 2 of an N-bit integer in O(lg(N)) operations with multiply and lookup
		/// </summary>
		public static int GetLog2(int value)
		{
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;

			return multiplyDeBruijnBitPosition[unchecked(((uint)value * 0x07C4ACDDU) >> 27)];
		}

		public static long Pack(int hiInt, int loInt)
		{
			unchecked
			{
				ulong packed;

				packed = (ulong)(uint)hiInt;
				packed <<= 32;
				packed |= (ulong)(uint)loInt;

				return (long)packed;
			}
		}

		public static void Unpack(long value, out int hiInt, out int loInt)
		{
			unchecked
			{
				loInt = (int)value;

				value >>= 32;
				hiInt = (int)value;
			}
		}
	}
}

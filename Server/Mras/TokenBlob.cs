using System;

namespace Mras
{
	public class TokenBlob1
	{
		public TokenBlob1()
		{
			MajorVersion = 1;
			MinorVersion = 0;
			Size = 0x0020;
			ClientID = new byte[20];
		}

		public byte MajorVersion;
		public byte MinorVersion;
		public UInt16 Size;
		public UInt32 ExpiryTimeLow;
		public UInt32 ExpiryTimeHigh;
		public byte[] ClientID;

		public virtual byte[] GetBytes(int extraLength)
		{
			byte[] bytes = new byte[Size + extraLength];
			int offset = 0;

			bytes[offset++] = MajorVersion;
			bytes[offset++] = MinorVersion;
			AddBytes(bytes, ref offset, Bigendian.GetBigendianBytes(Size));
			AddBytes(bytes, ref offset, Bigendian.GetBigendianBytes(ExpiryTimeLow));
			AddBytes(bytes, ref offset, Bigendian.GetBigendianBytes(ExpiryTimeHigh));
			AddBytes(bytes, ref offset, ClientID);

			return bytes;
		}

		protected static void AddBytes(byte[] dst, ref int offset, byte[] src)
		{
			Array.Copy(src, 0, dst, offset, src.Length);
			offset += src.Length;
		}
	}

	public class TokenBlob2
		: TokenBlob1
	{
		public UInt32 Flags;

		public TokenBlob2()
			: base()
		{
			MajorVersion = 2;
			MinorVersion = 0;
			Size = 0x0024;
		}

		public override byte[] GetBytes(int extraLength)
		{
			byte[] bytes = base.GetBytes(extraLength);
			int offset = Size - sizeof(UInt32);

			AddBytes(bytes, ref offset, Bigendian.GetBigendianBytes(Flags));

			return bytes;
		}
	}
}

using System;
using Sip.Message;
using Base.Message;

namespace System.Security.Cryptography
{
	public static class HashAlgorithmHelpers
	{
		public static void TransformBlock(this ICryptoTransform hash, ByteArrayPart part)
		{
			hash.TransformBlock(part.Bytes, part.Offset, part.Length, null, 0);
		}

		public static void TransformBlock(this ICryptoTransform hash, byte[] bytes)
		{
			hash.TransformBlock(bytes, 0, bytes.Length, null, 0);
		}

		public static void TransformFinalBlock(this ICryptoTransform hash, ByteArrayPart part)
		{
			hash.TransformFinalBlock(part.Bytes, part.Offset, part.Length);
		}

		public static void TransformFinalBlock(this ICryptoTransform hash, byte[] bytes)
		{
			hash.TransformFinalBlock(bytes, 0, bytes.Length);
		}
	}
}

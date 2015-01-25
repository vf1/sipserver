using System;
using System.Collections.Generic;

namespace Server.Http
{
	public enum Opcodes
	{
		Continuation = 0,
		Text = 1,
		Binary = 2,
		ConnectionClose = 8,
		Ping = 9,
		Pong = 10,
	}

	public struct WebSocketHeader
	{
		public bool Fin;
		public bool Mask;
		public Opcodes Opcode;
		public long PayloadLengthLong;
		public byte MaskingKey0;
		public byte MaskingKey1;
		public byte MaskingKey2;
		public byte MaskingKey3;

		public const int MaximumHeaderLength = 14;

		private States state;

		public int PayloadLength
		{
			get { return (int)PayloadLengthLong; }
			set { PayloadLengthLong = value; }
		}

		enum States
		{
			Byte0,
			Byte1,
			Payload126_0,
			Payload126_1,
			Payload127_0,
			Payload127_1,
			Payload127_2,
			Payload127_3,
			Mask0,
			Mask1,
			Mask2,
			Mask3,
			Done,
		}

		public int Parse(ArraySegment<byte> data)
		{
			return Parse(data.Array, data.Offset, data.Count);
		}

		public bool IsDone
		{
			get { return state == States.Done; }
		}

		public int Parse(byte[] bytes, int offset, int length)
		{
			int index;

			if (state == States.Done)
				state = States.Byte0;

			for (index = 0; index < length && state != States.Done; index++)
			{
				byte value = bytes[offset + index];

				switch (state)
				{
					case States.Byte0:
						Fin = (value & 0x80) != 0;
						Opcode = (Opcodes)(value & 0x0f);
						state = States.Byte1;
						break;

					case States.Byte1:
						Mask = (value & 0x80) != 0;
						PayloadLengthLong = value & 0x7f;
						if (PayloadLengthLong == 126)
							state = States.Payload126_0;
						else if (PayloadLengthLong == 127)
							state = States.Payload127_0;
						else
							state = Mask ? States.Mask0 : States.Done;
						break;

					case States.Payload126_0:
						PayloadLengthLong = value;
						state = States.Payload126_1;
						break;

					case States.Payload126_1:
						PayloadLengthLong <<= 8;
						PayloadLengthLong |= value;
						state = Mask ? States.Mask0 : States.Done;
						break;

					case States.Payload127_0:
						PayloadLengthLong = value;
						state = States.Payload127_1;
						break;

					case States.Payload127_1:
						PayloadLengthLong <<= 8;
						PayloadLengthLong |= value;
						state = States.Payload127_2;
						break;

					case States.Payload127_2:
						PayloadLengthLong <<= 8;
						PayloadLengthLong |= value;
						state = States.Payload127_3;
						break;

					case States.Payload127_3:
						PayloadLengthLong <<= 8;
						PayloadLengthLong |= value;
						state = Mask ? States.Mask0 : States.Done;
						break;

					case States.Mask0:
						MaskingKey0 = value;
						state = States.Mask1;
						break;

					case States.Mask1:
						MaskingKey1 = value;
						state = States.Mask2;
						break;

					case States.Mask2:
						MaskingKey2 = value;
						state = States.Mask3;
						break;

					case States.Mask3:
						MaskingKey3 = value;
						state = States.Done;
						break;
				}
			}

			return index;
		}

		public void MaskData(byte[] bytes, int offset, int length)
		{
			int index = 0;
			UnmaskData(bytes, offset, length, ref index);
		}

		public void UnmaskData(ArraySegment<byte> data, ref int maskIndex)
		{
			UnmaskData(data.Array, data.Offset, data.Count, ref maskIndex);
		}

		public void UnmaskData(byte[] bytes, int offset, int length, ref int maskIndex)
		{
			for (int end = offset + length; offset < end; )
				bytes[offset++] ^= GetMaskingKey(maskIndex++);
		}

		private byte GetMaskingKey(int index)
		{
			switch (index % 4)
			{
				case 0: return MaskingKey0;
				case 1: return MaskingKey1;
				case 2: return MaskingKey2;
				case 3: return MaskingKey3;
			}

			throw new InvalidProgramException();
		}

		public int GetHeaderLength()
		{
			return
				2 +
				((PayloadLengthLong > 126) ? 2 : 0) +
				((PayloadLengthLong > 65535) ? 6 : 0) +
				(Mask ? 4 : 0);
		}

		public void GenerateHeader(ArraySegment<byte> data)
		{
			GenerateHeader(data.Array, data.Offset, data.Count);
		}

		public void GenerateHeader(byte[] bytes, int offset, int length)
		{
			bytes[offset] = 0;
			if (Fin)
				bytes[offset] |= 0x80;
			bytes[offset] |= (byte)((int)Opcode & 0x0f);

			offset++;


			bytes[offset] = 0;
			if (Mask)
				bytes[offset] |= 0x80;

			if (PayloadLengthLong < 126)
			{
				bytes[offset++] |= (byte)PayloadLengthLong;
			}
			else if (PayloadLengthLong <= 65535)
			{
				bytes[offset++] |= 0x7e;

				bytes[offset++] = (byte)(PayloadLengthLong >> 8);
				bytes[offset++] = (byte)PayloadLengthLong;
			}
			else
			{
				bytes[offset++] |= 0x7f;

				for (int i = 7; i >= 0; i--)
					bytes[offset++] = (byte)(PayloadLengthLong >> (i * 8));
			}


			if (Mask)
			{
				bytes[offset++] = MaskingKey0;
				bytes[offset++] = MaskingKey1;
				bytes[offset++] = MaskingKey2;
				bytes[offset++] = MaskingKey3;
			}
		}

		public override string ToString()
		{
			return string.Format("Fin: {0}, Mask: {1}, Opcode: {2}, PayloadLength: {3}",//, MaskingKey: {4:x2}{5:x2}{6:x2}{7:x2}",
				Fin, Mask, Opcode, PayloadLengthLong, MaskingKey0, MaskingKey1, MaskingKey2, MaskingKey3);
		}
	}
}

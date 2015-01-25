using System;
using Base.Message;
using Sip.Message;

namespace Sip.Server
{
	class BufferManagerProxy
		: IBufferManager
	{
		public ArraySegment<byte> Allocate(int size)
		{
			return SocketServers.BufferManager.Allocate(size);
		}

		public void Reallocate(ref ArraySegment<byte> segment, int extraSize)
		{
			var newSegment = SocketServers.BufferManager.Allocate(segment.Count + extraSize);

			Buffer.BlockCopy(segment.Array, segment.Offset, newSegment.Array, newSegment.Offset, segment.Count);

			SocketServers.BufferManager.Free(ref segment);

			segment = newSegment;
		}

		public void Free(ref ArraySegment<byte> segment)
		{
			SocketServers.BufferManager.Free(ref segment);
		}
	}
}

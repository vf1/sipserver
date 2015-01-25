using System;
using SocketServers;

namespace Sip.Server
{
	public struct BufferHandle
	{
		private ArraySegment<byte> segment1;
		private ArraySegment<byte> segment2;

		public BufferHandle(ArraySegment<byte> segment1, ArraySegment<byte> segment2)
		{
			this.segment1 = segment1;
			this.segment2 = segment2;
		}

		public void Free()
		{
			BufferManager.Free(ref segment1);
			BufferManager.Free(ref segment2);
		}
	}
}

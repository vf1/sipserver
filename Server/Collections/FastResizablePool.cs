using System;
using System.Collections.Generic;
using System.Threading;
using SocketServers;

namespace System.Collections.Generic
{
	public sealed class FastResizablePool<T>
		: ILockFreePool<T>
		where T : class, IDisposable, ILockFreePoolItem, ILockFreePoolItemIndex, new()
	{
		private object sync;
		private int poolIndex;
		private LockFreeFastPool<T>[] pools;

		private const int minSize = 256;
		private const int maxPoolsQty = 16;

		public FastResizablePool()
		{
			poolIndex = 0;
			sync = new object();
			pools = new LockFreeFastPool<T>[maxPoolsQty];
			pools[0] = new LockFreeFastPool<T>(minSize);
		}

		public void Dispose()
		{
			for (int i = 1; i < maxPoolsQty; i++)
				if (pools[i] != null)
					pools[i].Dispose();
		}

		public T Get()
		{
			for (int i = Thread.VolatileRead(ref poolIndex); i >= 0; i--)
			{
				T value = pools[i].GetIfSpaceAvailable();
				if (value != default(T))
				{
					value.Index |= i << 24;
					return value;
				}
			}

			lock (sync)
			{
				for (; ; )
				{
					int index = Thread.VolatileRead(ref poolIndex);

					T value = pools[index].GetIfSpaceAvailable();
					if (value != default(T))
					{
						value.Index |= index << 24;
						return value;
					}

					int newIndex = Thread.VolatileRead(ref poolIndex) + 1;
					pools[newIndex] = new LockFreeFastPool<T>(minSize << newIndex);
					Thread.VolatileWrite(ref poolIndex, newIndex);
				}
			}
		}

		public void Put(ref T item)
		{
			Put(item);
			item = default(T);
		}

		public void Put(T value)
		{
			int index = value.Index >> 24;
			value.Index &= 0x00ffffff;

			pools[index].Put(value);
		}

		public int Created
		{
			get { throw new NotImplementedException(); }
		}

		public int Queued
		{
			get { throw new NotImplementedException(); }
		}
	}
}

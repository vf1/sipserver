using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Collections
{
	[TestFixture]
	class BinaryHeapTest
	{
		[Test]
		public void It_should_add_sorted_items()
		{
			var result = AddAndGetArray(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, });
			Assert.AreEqual(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, }, result);
		}

		[Test]
		public void It_should_add_reverse_sorted_items()
		{
			var result = AddAndGetArray(new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, });
			Assert.AreEqual(new int[] { 0, 1, 4, 3, 2, 8, 5, 9, 6, 7, }, result);
		}

		[Test]
		public void It_delete_min_item()
		{
			var list = new List<int>();
			var heap = new BinaryHeap<int>(list, MinHeapIntComparer.Default);

			heap.AddRange(new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, });

			Assert.AreEqual(0, heap.DeleteRoot());
			Assert.AreEqual(new int[] { 1, 2, 4, 3, 7, 8, 5, 9, 6, }, list.ToArray());
			Assert.AreEqual(1, heap.DeleteRoot());
			Assert.AreEqual(new int[] { 2, 3, 4, 6, 7, 8, 5, 9, }, list.ToArray());
			Assert.AreEqual(2, heap.DeleteRoot());
			Assert.AreEqual(new int[] { 3, 6, 4, 9, 7, 8, 5, }, list.ToArray());
			Assert.AreEqual(3, heap.DeleteRoot());
			Assert.AreEqual(new int[] { 4, 6, 5, 9, 7, 8, }, list.ToArray());
			Assert.AreEqual(4, heap.DeleteRoot());
			Assert.AreEqual(new int[] { 5, 6, 8, 9, 7, }, list.ToArray());
			Assert.AreEqual(5, heap.DeleteRoot());
			Assert.AreEqual(new int[] { 6, 7, 8, 9, }, list.ToArray());
			Assert.AreEqual(6, heap.DeleteRoot());
			Assert.AreEqual(new int[] { 7, 9, 8, }, list.ToArray());
			Assert.AreEqual(7, heap.DeleteRoot());
			Assert.AreEqual(new int[] { 8, 9, }, list.ToArray());
			Assert.AreEqual(8, heap.DeleteRoot());
			Assert.AreEqual(new int[] { 9, }, list.ToArray());
			Assert.AreEqual(9, heap.DeleteRoot());
			Assert.AreEqual(0, heap.Count);
		}

		private static int[] AddAndGetArray(int[] data)
		{
			var list = new List<int>();
			var heap = new BinaryHeap<int>(list, MinHeapIntComparer.Default);

			heap.AddRange(data);

			return list.ToArray();
		}

		class MinHeapIntComparer
			: Comparer<int>
		{
			private static readonly IComparer<int> defaultComparer = new MinHeapIntComparer();

			public static new IComparer<int> Default
			{
				get { return defaultComparer; }
			}

			public override int Compare(int x, int y)
			{
				if (x > y)
					return -1;
				if (x < y)
					return 1;
				return 0;
			}
		}
	}
}

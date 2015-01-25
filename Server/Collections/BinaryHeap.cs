using System;
using System.Collections.Generic;

namespace System.Collections.Generic
{
	class BinaryHeap<T>
	{
		private IList<T> list;
		private IComparer<T> comparer;

		public BinaryHeap(IList<T> list, IComparer<T> comparer)
		{
			this.list = list;
			this.comparer = (comparer != null) ? comparer : Comparer<T>.Default;
		}

		public void AddRange(IEnumerable<T> values)
		{
			foreach (var value in values)
				Add(value);
		}

		public void Add(T value)
		{
			list.Add(value);

			int newone = Count - 1;
			int parent = (newone - 1) / 2;

			while (newone > 0 && comparer.Compare(list[parent], list[newone]) < 0)
			{
				T tempValue = list[newone];
				list[newone] = list[parent];
				list[parent] = tempValue;

				newone = parent;
				parent = (newone - 1) / 2;
			}
		}

		public T DeleteRoot()
		{
			T result = list[0];

			int last = Count - 1;

			list[0] = list[last];
			list.RemoveAt(last);

			Heapify(0);

			return result;
		}

		public T PeekRoot()
		{
			return list[0];
		}

		public void Heapify(int index)
		{
			int leftChild, rightChild, minChild;
			int count = Count;

			for (; ; )
			{
				leftChild = 2 * index + 1;
				rightChild = 2 * index + 2;
				minChild = index;

				if (leftChild < count && comparer.Compare(list[leftChild], list[minChild]) > 0)
					minChild = leftChild;

				if (rightChild < count && comparer.Compare(list[rightChild], list[minChild]) > 0)
					minChild = rightChild;

				if (minChild == index)
					break;

				T tempValue = list[index];
				list[index] = list[minChild];
				list[minChild] = tempValue;

				index = minChild;
			}
		}

		public int Count
		{
			get { return list.Count; }
		}
	}
}

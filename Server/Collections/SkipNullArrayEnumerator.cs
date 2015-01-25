using System;
using System.Collections.Generic;

namespace System.Collections.Generic
{
	class SkipNullArrayEnumerator<T>
		: IEnumerator<T>
		where T : class
	{
		private int index;
		private T[] array;

		public SkipNullArrayEnumerator(int capacity)
		{
			array = new T[capacity];
		}

		public void Initialize(T[] array)
		{
			index = -1;

			if (this.array == null || this.array.Length < array.Length)
				this.array = new T[array.Length];

			for (int i = 0, j = 0; i < array.Length; i++)
				if (array[i] != default(T))
					this.array[j++] = array[i];
		}

		public void Dispose()
		{
			Array.Clear(array, 0, array.Length);
		}

		public bool MoveNext()
		{
			return ++index < array.Length && array[index] != default(T);
		}

		public void Reset()
		{
			index = -1;
		}

		public T Current
		{
			get { return array[index]; }
		}

		Object IEnumerator.Current
		{
			get { return array[index]; }
		}
	}
}

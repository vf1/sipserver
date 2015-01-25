using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Collections.Generic
{
	class EmptyEnumerable<T>
		: IEnumerable<T>
	{
		public static readonly IEnumerable<T> Instance = new EmptyEnumerable<T>();

		public IEnumerator<T> GetEnumerator()
		{
			return EmptyEnumerator<T>.Instance;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return EmptyEnumerator<T>.Instance;
		}
	}

	class EmptyEnumerator<T>
		: IEnumerator<T>
	{
		public static readonly IEnumerator<T> Instance = new EmptyEnumerator<T>();

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			return false;
		}

		public void Reset()
		{
		}

		public T Current
		{
			get { throw new InvalidOperationException(); }
		}

		Object IEnumerator.Current
		{
			get { throw new InvalidOperationException(); }
		}
	}
}

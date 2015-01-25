using System;
using System.Collections;
using System.Collections.Generic;

namespace Sip.Server
{
	class TypedEnumerator<T>
		: IEnumerator<T>
	{
		IEnumerator enumerator;

		public TypedEnumerator(IEnumerator enumerator1)
		{
			enumerator = enumerator1;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			return enumerator.MoveNext();
		}

		public void Reset()
		{
			enumerator.Reset();
		}

		Object IEnumerator.Current
		{
			get { return enumerator.Current; }
		}

		T IEnumerator<T>.Current
		{
			get { return (T)enumerator.Current; }
		}
	}
}

using System;

namespace Sip.Server
{
	public struct TupleStruct<T1, T2>
		: IEquatable<TupleStruct<T1, T2>>
	{
		private readonly T1 First;
		private readonly T2 Second;

		public TupleStruct(T1 f, T2 s)
		{
			First = f;
			Second = s;
		}

		public bool Equals(TupleStruct<T1, T2> other)
		{
			return First.Equals(other.First) && Second.Equals(other.Second);
		}

		public override bool Equals(object obj)
		{
			if (obj is TupleStruct<T1, T2>)
				return this.Equals((TupleStruct<T1, T2>)obj);
			else
				return false;
		}

		public override int GetHashCode()
		{
			return First.GetHashCode() ^ Second.GetHashCode();
		}
	}
}

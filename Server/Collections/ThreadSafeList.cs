using System;
using System.Threading;
using System.Collections.Generic;

namespace System.Collections.Generic.ThreadSafe
{
	struct List<T>
	{
		private IList<T> list;

		public List(IList<T> list)
		{
			this.list = list;
		}

		public void AddIfAbsent(T item)
		{
			lock (list)
			{
				if (list.IndexOf(item) < 0)
					list.Add(item);
			}
		}

		public void Replace(T item)
		{
			lock (list)
			{
				list.Remove(item);
				list.Add(item);
			}
		}

		public bool Remove(T item)
		{
			bool result;

			lock (list)
				result = list.Remove(item);

			return result;
		}

		public bool RemoveFirst<P>(P param, Func<T, P, bool> predicate)
		{
			bool result = false;

			lock (list)
			{
				for (int i = 0; i < list.Count; i++)
					if (predicate(list[i], param))
					{
						list.RemoveAt(i);
						result = true;
						break;
					}
			}

			return result;
		}

		public void ForEach(Action<T> action)
		{
			lock (list)
			{
				for (int i = 0; i < list.Count; i++)
					action(list[i]);
			}
		}

		public void RecursiveForEach(Action<T> action)
		{
			Monitor.Enter(list);

			if (list.Count > 0)
				RecursiveForEach(action, 0);
			else
				Monitor.Exit(list);
		}

		public void RecursiveForEach(Action<T> action, int index)
		{
			var item = list[index++];

			if (index < list.Count)
				RecursiveForEach(action, index);
			else
				Monitor.Exit(list);

			action(item);
		}
	}
}

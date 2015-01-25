using System;
using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Generic.ThreadSafe
{
	public class Dictionary<K, T>
		: IDisposable
	{
		private ReaderWriterLockSlim sync;
		private IDictionary<K, T> dictionary;

		public Dictionary()
			: this(new System.Collections.Generic.Dictionary<K, T>())
		{
		}

		public Dictionary(int capacity)
			: this(new System.Collections.Generic.Dictionary<K, T>(capacity))
		{
		}

		public Dictionary(IDictionary<K, T> dictionary)
		{
			this.sync = new ReaderWriterLockSlim();
			this.dictionary = dictionary;
		}

		public void Dispose()
		{
			sync.Dispose();
		}

		public void Clear()
		{
			try
			{
				sync.EnterWriteLock();
				dictionary.Clear();
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public void Add(K key, T value)
		{
			try
			{
				sync.EnterWriteLock();
				dictionary.Add(key, value);
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public bool TryAdd(K key, T value)
		{
			try
			{
				sync.EnterWriteLock();

				if (dictionary.ContainsKey(key))
					return false;

				dictionary.Add(key, value);

				return true;
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public T GetOrAdd(K key, T value)
		{
			try
			{
				sync.EnterWriteLock();

				if (dictionary.ContainsKey(key))
					return dictionary[key];

				dictionary.Add(key, value);

				return value;
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public T GetOrAdd(K key, Func<K, T> factory)
		{
			try
			{
				sync.EnterWriteLock();

				T value;
				if (dictionary.TryGetValue(key, out value) == false)
				{
					value = factory(key);
					dictionary.Add(key, value);
				}

				return value;
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public bool GetOrAdd<A1, A2>(K key, A1 a1, A2 a2, Func<K, A1, A2, T> factory, out T value)
		{
			try
			{
				sync.EnterWriteLock();

				if (dictionary.TryGetValue(key, out value) == false)
				{
					value = factory(key, a1, a2);
					dictionary.Add(key, value);
					return false;
				}

				return true;
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public T GetOrAdd<A1, A2>(K key, A1 a1, A2 a2, Func<K, A1, A2, T> factory)
		{
			T value;
			GetOrAdd<A1, A2>(key, a1, a2, factory, out value);

			return value;
		}

		public T Replace(K key, T value)
		{
			try
			{
				sync.EnterWriteLock();

				T oldValue;
				if (dictionary.TryGetValue(key, out oldValue))
					dictionary.Remove(key);

				dictionary.Add(key, value);

				return oldValue;
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public void Remove(Predicate<T> predicate)
		{
			Remove((key, value) => { return predicate(value); }, null, 32);
		}

		public void Remove(Func<K, T, bool> predicate, Action<K, T> removedEvent, int lockedSize)
		{
			var removeList = new System.Collections.Generic.List<KeyValuePair<K, T>>(dictionary.Count / 2);
			var removed = (removedEvent != null) ? new bool[lockedSize] : null;

			try
			{
				sync.EnterReadLock();

				foreach (var pair in dictionary)
					if (predicate(pair.Key, pair.Value))
						removeList.Add(pair);
			}
			finally
			{
				sync.ExitReadLock();
			}


			for (int j = 0; j < removeList.Count; j++)
			{
				int start2 = j * lockedSize;
				int count2 = start2 + lockedSize;
				if (count2 > removeList.Count)
					count2 = removeList.Count;

				try
				{
					sync.EnterWriteLock();

					for (int i = start2; i < count2; i++)
					{
						var current = removeList[i];

						bool reallyRemoved = false;

						if (predicate(current.Key, current.Value))
							reallyRemoved = dictionary.Remove(current);

						if (removed != null)
							removed[i] = reallyRemoved;
					}
				}
				finally
				{
					sync.ExitWriteLock();
				}

				if (removedEvent != null)
				{
					for (int i = start2; i < count2; i++)
						if (removed[i])
							removedEvent(removeList[i].Key, removeList[i].Value);
				}
			}
		}

		// bad for GC too many root item (var pair) while recursive function
		//public void Remove(Predicate<T> predicate)
		//{
		//    try
		//    {
		//        sync.EnterWriteLock();

		//        using (var enumerator = dictionary.GetEnumerator())
		//        {
		//            RecursiveRemove(enumerator, predicate);
		//        }
		//    }
		//    finally
		//    {
		//        sync.ExitWriteLock();
		//    }
		//}

		//private void RecursiveRemove(IEnumerator<KeyValuePair<K, T>> enumerator, Predicate<T> predicate)
		//{
		//    if (enumerator.MoveNext())
		//    {
		//        var pair = enumerator.Current;
		//        RecursiveRemove(enumerator, predicate);

		//        if (predicate(pair.Value))
		//            dictionary.Remove(pair.Key);
		//    }
		//}

		//public bool Remove(K key, T value)
		//{
		//    bool result = false;
		//    try
		//    {
		//        sync.EnterWriteLock();

		//        T dictValue;
		//        if (dictionary.TryGetValue(key, out dictValue))
		//        {
		//            if (dictValue == value)
		//                result = dictionary.Remove(key);
		//        }
		//    }
		//    finally
		//    {
		//        sync.ExitWriteLock();
		//    }

		//    return result;
		//}

		public IList<T> ToList()
		{
			try
			{
				sync.EnterReadLock();

				var list = new System.Collections.Generic.List<T>(dictionary.Count);

				foreach (var pair in dictionary)
					list.Add(pair.Value);

				return list;
			}
			finally
			{
				sync.ExitReadLock();
			}
		}

		public T First(Func<T, bool> predicate)
		{
			try
			{
				sync.EnterReadLock();
				foreach (var pair in dictionary)
					if (predicate(pair.Value))
						return pair.Value;

				return default(T);
			}
			finally
			{
				sync.ExitReadLock();
			}
		}

		public void ForEach(Action<T> action)
		{
			try
			{
				sync.EnterReadLock();
				foreach (var pair in dictionary)
					action(pair.Value);
			}
			finally
			{
				sync.ExitReadLock();
			}
		}

		public bool Contain(Func<T, bool> predicate)
		{
			try
			{
				sync.EnterReadLock();
				foreach (var pair in dictionary)
					if (predicate(pair.Value))
						return true;
			}
			finally
			{
				sync.ExitReadLock();
			}

			return false;
		}

		public void Remove(K key)
		{
			try
			{
				sync.EnterWriteLock();
				dictionary.Remove(key);
			}
			finally
			{
				sync.ExitWriteLock();
			}
		}

		public bool TryGetValue(K key, out T value)
		{
			bool result;

			try
			{
				sync.EnterReadLock();
				result = dictionary.TryGetValue(key, out value);
			}
			finally
			{
				sync.ExitReadLock();
			}

			return result;
		}

		public T TryGetValue(K key)
		{
			T value;

			try
			{
				sync.EnterReadLock();
				dictionary.TryGetValue(key, out value);
			}
			finally
			{
				sync.ExitReadLock();
			}

			return value;
		}

		public T GetValue(K key)
		{
			T value;
			try
			{
				sync.EnterReadLock();
				dictionary.TryGetValue(key, out value);
			}
			finally
			{
				sync.ExitReadLock();
			}

			return value;
		}

		public bool ContainsKey(K key)
		{
			bool result;
			try
			{
				sync.EnterReadLock();
				result = dictionary.ContainsKey(key);
			}
			finally
			{
				sync.ExitReadLock();
			}

			return result;
		}
	}
}

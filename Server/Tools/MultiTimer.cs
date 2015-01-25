using System;
using System.Threading;
using System.Collections.Generic;

namespace Sip.Tools
{
	sealed class MultiTimerEx<T>
		: MultiTimer<T>
		where T : IEquatable<T>
	{
		public MultiTimerEx(Action<T> timerCallback, int capacity)
			: base(timerCallback, capacity, false)
		{
		}

		public MultiTimerEx(Action<T> timerCallback, int capacity, int defaultDelay)
			: base(timerCallback, capacity, defaultDelay)
		{
		}

		public void Remove(int stateIndex, T param)
		{
			lock (sync)
			{
				if (states[stateIndex].Param.Equals(param))
					base.Remove(stateIndex);
			}
		}

		public int Change(int stateIndex, T param, int newDelay)
		{
			lock (sync)
			{
				if (states[stateIndex].Param.Equals(param))
					base.Remove(stateIndex);
				return base.Add(newDelay, param);
			}
		}

		public int Change(int stateIndex, T param)
		{
			lock (sync)
			{
				if (states[stateIndex].Param.Equals(param))
					base.Remove(stateIndex);
				return base.Add(param);
			}
		}
	}

	class MultiTimer<T>
		: IDisposable
	{
		#region struct State {...}

		protected struct State
		{
			public readonly bool Enabled;
			public readonly int StopTickCount;
			public readonly T Param;

			public State(int stopTickCount, T param)
			{
				Enabled = true;
				StopTickCount = stopTickCount;
				Param = param;
			}
		}

		#endregion

		#region struct DueTime {...}

		protected struct DueTime
		{
			public readonly int StopTickCount;
			public readonly int StateIndex;

			public DueTime(int stopTickCount, int stateIndex)
			{
				StopTickCount = stopTickCount;
				StateIndex = stateIndex;
			}

			public int GetLeftTime(int tickCount)
			{
				return unchecked(StopTickCount - tickCount);
			}

			public int GetLeftTime()
			{
				return unchecked(StopTickCount - Environment.TickCount);
			}
		}

		#endregion

		#region class DueTimeComparer {...}

		protected class DueTimeComparer
			: IComparer<DueTime>
		{
			public DueTimeComparer()
			{
				BaseTickCount = Environment.TickCount;
			}

			public int BaseTickCount;

			public int Compare(DueTime x, DueTime y)
			{
				int leftTimeX = unchecked(x.StopTickCount - BaseTickCount);
				int leftTimeY = unchecked(y.StopTickCount - BaseTickCount);

				if (leftTimeX > leftTimeY)
					return -1;
				if (leftTimeX < leftTimeY)
					return 1;
				return 0;
			}
		}

		#endregion

		protected readonly object sync;
		protected readonly Timer timer;
		protected readonly List<State> states;
		protected readonly Queue<int> freeStateIndexes;
		protected readonly DueTimeComparer dueTimeComparer;
		protected readonly BinaryHeap<DueTime> dueTimes;
		protected readonly bool accessByParamEnabled;
		protected readonly Dictionary<T, int> paramStateIndexes;
		protected readonly Action<int, T> timerCallback;
		protected readonly Action<T> timerCallbackOld;
		protected readonly int defaultDelay;

		public readonly int InvalidTimerIndex = -1;

		public MultiTimer(Action<T> timerCallback, int capacity)
			: this(timerCallback, capacity, false)
		{
		}

		public MultiTimer(Action<T> timerCallback, int capacity, bool enableRemoveByParam)
			: this(timerCallback, capacity, enableRemoveByParam, null)
		{
		}

		public MultiTimer(Action<T> timerCallback, int capacity, int defaultDelay)
			: this(timerCallback, capacity, false, null, defaultDelay)
		{
		}

		public MultiTimer(Action<int, T> timerCallback, int capacity, int defaultDelay)
			: this(timerCallback, capacity, false, null, defaultDelay)
		{
		}

		public MultiTimer(Action<int, T> timerCallback, int capacity, bool enableRemoveByParam, int defaultDelay)
			: this(timerCallback, capacity, enableRemoveByParam, null, defaultDelay)
		{
		}

		public MultiTimer(Action<T> timerCallback, int capacity, bool enableRemoveByParam, IEqualityComparer<T> paramComparer)
			: this(timerCallback, capacity, enableRemoveByParam, paramComparer, 0)
		{
		}

		public MultiTimer(Action<T> timerCallback, int capacity, bool enableRemoveByParam, IEqualityComparer<T> paramComparer, int defaultDelay)
			: this(new ActionClosure(timerCallback).Caller, capacity, enableRemoveByParam, paramComparer, defaultDelay)
		{
		}

		#region class ActionClosure {...}

		class ActionClosure
		{
			private Action<T> action;

			public ActionClosure(Action<T> action)
			{
				this.action = action;
			}

			public void Caller(int startIndex, T param)
			{
				action(param);
			}
		}

		#endregion

		public MultiTimer(Action<int, T> timerCallback, int capacity, bool enableRemoveByParam, IEqualityComparer<T> paramComparer, int defaultDelay)
		{
			this.timerCallback = timerCallback;
			this.accessByParamEnabled = enableRemoveByParam;
			this.defaultDelay = defaultDelay;

			sync = new object();
			timer = new Timer(new TimerCallback(TimerCallbackHandler));
			states = new List<State>(capacity);
			freeStateIndexes = new Queue<int>(capacity);
			dueTimeComparer = new DueTimeComparer();
			dueTimes = new BinaryHeap<DueTime>(new List<DueTime>(capacity), dueTimeComparer);

			if (enableRemoveByParam)
				paramStateIndexes = new Dictionary<T, int>(capacity, paramComparer);
		}

		public void Dispose()
		{
			lock (sync)
			{
				timer.Dispose();
				states.Clear();
			}
		}

		public int Add(T param)
		{
			return Add(defaultDelay, param);
		}

		public int Add(int delay, T param)
		{
			lock (sync)
			{
				int tickCount = Environment.TickCount;
				int stopTickCount = unchecked(tickCount + delay);

				int stateIndex = CreateState(stopTickCount, param);

				int minDelay = (dueTimes.Count > 0) ?
					dueTimes.PeekRoot().GetLeftTime(tickCount) : int.MaxValue;

				dueTimes.Add(new DueTime(stopTickCount, stateIndex));

				if (delay < minDelay)
					timer.Change(delay, Timeout.Infinite);

				if (accessByParamEnabled)
					paramStateIndexes.Add(param, stateIndex);

				return stateIndex;
			}
		}

		private int CreateState(int stopTickCount, T param)
		{
			int index;

			if (freeStateIndexes.Count > 0)
			{
				index = freeStateIndexes.Dequeue();
				states[index] = new State(stopTickCount, param);
			}
			else
			{
				index = states.Count;
				states.Add(new State(stopTickCount, param));
			}

			return index;
		}

		public void Remove(int stateIndex)
		{
			lock (sync)
			{
				if (accessByParamEnabled)
					paramStateIndexes.Remove(states[stateIndex].Param);

				states[stateIndex] = new State();

				freeStateIndexes.Enqueue(stateIndex);
			}
		}

		public void RemoveByParam(T param)
		{
			lock (sync)
			{
				int stateIndex;
				if (paramStateIndexes.TryGetValue(param, out stateIndex))
				{
					paramStateIndexes.Remove(param);
					states[stateIndex] = new State();

					freeStateIndexes.Enqueue(stateIndex);
				}
			}
		}

		public void Remove(T param)
		{
			RemoveByParam(param);
		}

		public void Change(T param, int newDelay)
		{
			lock (sync)
			{
				RemoveByParam(param);
				Add(newDelay, param);
			}
		}

		private void TimerCallbackHandler(object none)
		{
			int stateIndex = -1;
			T param = default(T);
			bool callback = false;

			lock (sync)
			{
				var dueTime = dueTimes.DeleteRoot();

				stateIndex = dueTime.StateIndex;

				if (states[stateIndex].Enabled && states[stateIndex].StopTickCount == dueTime.StopTickCount)
				{
					callback = true;
					param = states[stateIndex].Param;

					if (accessByParamEnabled)
						paramStateIndexes.Remove(states[stateIndex].Param);

					states[stateIndex] = new State();
					freeStateIndexes.Enqueue(stateIndex);
				}

				dueTimeComparer.BaseTickCount = dueTime.StopTickCount;

				if (dueTimes.Count > 0)
				{
					int leftTime = dueTimes.PeekRoot().GetLeftTime(Environment.TickCount);
					timer.Change((leftTime > 0) ? leftTime : 0, Timeout.Infinite);
				}
			}

			if (callback)
				timerCallback(stateIndex, param);
		}
	}
}

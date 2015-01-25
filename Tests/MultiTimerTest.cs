using System;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using Sip.Tools;

namespace Test
{
	[TestFixture]
	class MultiTimerTest
	{
		struct TickCounts
		{
			public int Real;
			public int Expected;
		}

		private List<TickCounts> results = new List<TickCounts>(256);

		[Test]
		public void It_should_callback_after_due_time()
		{
			results.Clear();

			using (var timer = new MultiTimer<int>(MultiTimerCalback, 256))
			{
				timer.Add(700, Environment.TickCount + 700);
				timer.Add(500, Environment.TickCount + 500);
				timer.Add(200, Environment.TickCount + 200);
				timer.Add(600, Environment.TickCount + 600);
				timer.Add(300, Environment.TickCount + 300);
				timer.Add(100, Environment.TickCount + 100);

				Validate(6);
			}
		}

		[Test]
		public void It_should_callback_after_due_time_for_same_delay()
		{
			results.Clear();

			using (var timer = new MultiTimer<int>(MultiTimerCalback, 256))
			{
				timer.Add(300, Environment.TickCount + 300);
				timer.Add(300, Environment.TickCount + 300);
				timer.Add(300, Environment.TickCount + 300);
				timer.Add(300, Environment.TickCount + 300);
				timer.Add(300, Environment.TickCount + 300);
				timer.Add(300, Environment.TickCount + 300);

				Validate(6);
			}
		}

		[Test]
		public void It_should_stop_timer_by_id()
		{
			results.Clear();

			using (var timer = new MultiTimer<int>(MultiTimerCalback, 256))
			{
				int id1 = timer.Add(700, Environment.TickCount + 700);
				int id2 = timer.Add(500, Environment.TickCount + 500);
				int id3 = timer.Add(200, Environment.TickCount + 200);
				int id4 = timer.Add(600, Environment.TickCount + 600);
				int id5 = timer.Add(300, Environment.TickCount + 300);
				int id6 = timer.Add(100, Environment.TickCount + 100);

				timer.Remove(id1);
				timer.Remove(id3);
				timer.Remove(id5);

				Validate(3);
			}
		}

		[Test]
		public void It_should_stop_timer_by_param()
		{
			results.Clear();

			using (var timer = new MultiTimer<long>(MultiTimerCalback, 256, true))
			{
				long[] param = new long[6];

				int id1 = timer.Add(700, param[0] = Environment.TickCount + 700);
				int id2 = timer.Add(500, param[1] = Environment.TickCount + 500);
				int id3 = timer.Add(200, param[2] = Environment.TickCount + 200);
				int id4 = timer.Add(600, param[3] = Environment.TickCount + 600);
				int id5 = timer.Add(300, param[4] = Environment.TickCount + 300);
				int id6 = timer.Add(100, param[5] = Environment.TickCount + 100);

				timer.Remove(param[0]);
				timer.Remove(param[2]);
				timer.Remove(param[4]);

				Validate(3);

				Assert.AreEqual((int)param[5], results[0].Expected);
				Assert.AreEqual((int)param[1], results[1].Expected);
				Assert.AreEqual((int)param[3], results[2].Expected);
			}
		}

		[Test]
		public void It_should_call_remove_by_id_if_T_is_int()
		{
			results.Clear();

			using (var timer = new MultiTimer<int>(MultiTimerCalback, 256, true))
			{
				int id = timer.Add(100, 12345);

				timer.RemoveByParam(12345);

				Thread.Sleep(200);
				Assert.AreEqual(0, results.Count);
			}
		}

		[Test]
		public void It_should_corect_work_when_internal_state_reused()
		{
			results.Clear();

			using (var timer = new MultiTimer<int>(MultiTimerCalback, 256))
			{
				int id1 = timer.Add(300, Environment.TickCount + 300);
				int id2 = timer.Add(300, Environment.TickCount + 300);
				int id3 = timer.Add(300, Environment.TickCount + 300);

				timer.Add(300, Environment.TickCount + 300);
				timer.Add(300, Environment.TickCount + 300);
				timer.Add(300, Environment.TickCount + 300);

				timer.Remove(id1);
				timer.Remove(id2);
				timer.Remove(id3);

				timer.Add(600, Environment.TickCount + 600);
				timer.Add(600, Environment.TickCount + 600);
				timer.Add(600, Environment.TickCount + 600);

				Validate(6);
			}
		}

		private void Validate(int count)
		{
			int start = Environment.TickCount;
			int reslutsCount = 0;
			while (reslutsCount < count)
			{
				Thread.Sleep(200);
				lock (results)
					reslutsCount = results.Count;
				if (unchecked(Environment.TickCount - start) > 10000)
					Assert.Fail("Test must finish during 10 seconds");
			}

			Assert.AreEqual(count, results.Count, "MultiTimer.Callback.Count");

			for (int i = 0; i < results.Count; i++)
			{
				Assert.LessOrEqual(results[i].Expected, results[i].Real);

				int difference = results[i].Real - results[i].Expected;
				Assert.LessOrEqual(difference, 20);
			}
		}

		private void MultiTimerCalback(long param)
		{
			MultiTimerCalback((int)param);
		}

		private void MultiTimerCalback(int param)
		{
			lock (results)
				results.Add(new TickCounts() { Real = Environment.TickCount, Expected = param, });
		}
	}
}

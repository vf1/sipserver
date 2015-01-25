using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sip.Server;
using Sip.Message;
using SocketServers;

namespace Test.Transaction1
{
	abstract class BaseServerTransactionTest<T>
		: BaseTransactionTest<T>
		where T : IServerTransaction, new()
	{
		protected void EmulateTuEvent(Transaction.States fromState, int startStatus, int endStatus, Transaction.States toState)
		{
			EmulateTuEvent(true, fromState, startStatus, endStatus, toState);
			EmulateTuEvent(false, fromState, startStatus, endStatus, toState);
		}

		protected void EmulateTuEvent(bool isTransportUnreliable, Transaction.States fromState, int startStatus, int endStatus, Transaction.States toState)
		{
			var e = GetServerEventArgs(isTransportUnreliable);

			for (int i = startStatus; i <= endStatus; i++)
			{
				var transaction = GetTransaction(fromState, isTransportUnreliable);
				transaction.ProccessTransactionUser(i, e);

				Assert.AreEqual(toState, transaction.State);
			}
		}

	}
}

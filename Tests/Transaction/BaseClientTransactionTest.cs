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
	abstract class BaseClientTransactionTest<T>
		: BaseTransactionTest<T>
		where T : IClientTransaction, new()
	{
		protected void EmulateTransportEvent(Transaction.States fromState, int startStatus, int endStatus, Transaction.States toState)
		{
			EmulateTransportEvent(true, fromState, startStatus, endStatus, toState);
			EmulateTransportEvent(false, fromState, startStatus, endStatus, toState);
		}

		protected void EmulateTransportEvent(bool isTransportUnreliable, Transaction.States fromState, int startStatus, int endStatus, Transaction.States toState)
		{
			for (int i = startStatus; i <= endStatus; i++)
			{
				var transaction = GetTransaction(fromState, isTransportUnreliable);
				transaction.ProccessTransport(i);

				Assert.AreEqual(toState, transaction.State);
			}
		}

		protected override T GetTransaction(Transaction.States state, bool isTransportUnreliable)
		{
			var e = GetServerEventArgs(isTransportUnreliable);

			var transaction = new T();

			if (Transaction.States.Calling != Transaction.States.Trying)
				throw new InvalidProgramException();

			switch (state)
			{
				case Transaction.States.Created:
					break;

				case Transaction.States.Trying:
					//case Transaction.States.Calling:
					transaction.ProccessTransactionUser(false, e);
					break;

				case Transaction.States.Proceeding:
					transaction.ProccessTransactionUser(false, e);
					transaction.ProccessTransport(100);
					break;

				case Transaction.States.Completed:
					if (isTransportUnreliable == false)
						throw new InvalidProgramException(@"States.Completed state accessable only for Unreable transport");
					transaction.ProccessTransactionUser(false, e);
					transaction.ProccessTransport(300);
					break;

				case Transaction.States.Terminated:
					transaction.ProccessTransactionUser(false, e);
					transaction.ProccessTransportError();
					break;
			}

			if (state != transaction.State)
				throw new InvalidProgramException();

			EventArgsManager.Put(e);

			return transaction;
		}
	}
}

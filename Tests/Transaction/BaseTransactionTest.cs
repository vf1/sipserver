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
	class BaseConnection2 : BaseConnection, IDisposable
	{
		void IDisposable.Dispose() { }
	}

	abstract class BaseTransactionTest<T>
		where T : ITransaction, new()
	{
		private ServersManager<BaseConnection2> serversManager;

		protected struct StateUnreliable
		{
			public StateUnreliable(Transaction.States state, bool isTransportUnreliable)
			{
				State = state;
				IsTransportUnreliable = isTransportUnreliable;
			}

			public readonly Transaction.States State;
			public readonly bool IsTransportUnreliable;
		}

		public BaseTransactionTest()
		{
			serversManager = new ServersManager<BaseConnection2>(new ServersManagerConfig());
		}

		protected abstract T GetTransaction(Transaction.States state, bool isTransportUnreliable);
		protected abstract IEnumerable<StateUnreliable> GetAllStates();

		protected IEnumerable<StateUnreliable> GetAllStatesFor(Transaction.States include)
		{
			return GetAllStatesFor(include, include);
		}

		protected IEnumerable<StateUnreliable> GetAllStatesFor(Transaction.States include1, Transaction.States include2)
		{
			foreach (var state in GetAllStates())
			{
				if (state.State == include1 || state.State == include2)
					yield return state;
			}
		}

		protected IEnumerable<StateUnreliable> GetAllStatesExcept(Transaction.States except)
		{
			return GetAllStatesExcept(except, except);
		}

		protected IEnumerable<StateUnreliable> GetAllStatesExcept(Transaction.States except1, Transaction.States except2)
		{
			foreach (var state in GetAllStates())
			{
				if (state.State == except1 && state.State == except2)
					yield return state;
			}
		}

		protected IEnumerable<T> GetAllTransactionsExcept(Transaction.States except)
		{
			return GetAllTransactionsExcept(except, except);
		}

		protected IEnumerable<T> GetAllTransactionsExcept(Transaction.States except1, Transaction.States except2)
		{
			foreach (var state in GetAllStates())
			{
				if (state.State != except1 && state.State != except2)
					yield return GetTransaction(state.State, state.IsTransportUnreliable);
			}
		}

		protected IEnumerable<T> GetAllTransactionsFor(Transaction.States include)
		{
			return GetAllTransactionsFor(include, include);
		}

		protected IEnumerable<T> GetAllTransactionsFor(Transaction.States include1, Transaction.States include2)
		{
			foreach (var state in GetAllStates())
			{
				if (state.State == include1 || state.State == include2)
					yield return GetTransaction(state.State, state.IsTransportUnreliable);
			}
		}

		protected static ServerAsyncEventArgs GetServerEventArgs(bool isTransportUnreliable)
		{
			var e = EventArgsManager.Get();

			e.Count = 100;
			e.AllocateBuffer();
			e.LocalEndPoint = new ServerEndPoint(isTransportUnreliable ? ServerProtocol.Udp : ServerProtocol.Tcp,
				new System.Net.IPEndPoint(System.Net.IPAddress.None, 0));

			return e;
		}
	}
}

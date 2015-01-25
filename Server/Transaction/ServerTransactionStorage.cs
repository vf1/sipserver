using System;
using System.Collections.Generic;
using Sip.Message;
using SocketServers;

namespace Sip.Server
{
	interface IRelaytedTransactionId
	{
		int GetRelaytedTransactionId(BadServerTransactionKey key);
	}

	sealed class ServerTransactionStorage<T>
		: IRelaytedTransactionId
		where T : IServerTransaction, new()
	{
		private readonly object sync;
		private readonly Transaction.Kind trasactionKind;
		private readonly Dictionary<int, T> transactions;
		private readonly Dictionary<BadServerTransactionKey, int> transactionIndexes;
		private readonly IRelaytedTransactionId inviteStorage;

		private int transactionCount;

		public Func<SipMessageReader, int> GetAcceptedRequestIndex;

		public ServerTransactionStorage(int capacity, Transaction.Kind kind, IRelaytedTransactionId inviteStorage)
		{
			this.sync = new object();
			this.trasactionKind = kind;
			this.transactions = new Dictionary<int, T>(capacity);
			this.transactionIndexes = new Dictionary<BadServerTransactionKey, int>(capacity);
			this.inviteStorage = inviteStorage;
		}

		public int ProccessTransport(SipMessageReader reader, out T transaction)
		{
			var key = new BadServerTransactionKey(reader, (reader.IsAck || reader.IsCancel) ? Methods.Invitem : reader.Method);

			lock (sync)
			{
				if (TryGet(key, out transaction) == false)
				{
					int transactionId = Transaction.InvalidKId;

					if (reader.Method == Methods.Cancelm)
						transactionId = Transaction.ChangeKind(
							inviteStorage.GetRelaytedTransactionId(key), Transaction.Kind.CancelServer);

					if (transactionId == Transaction.InvalidKId)
						transactionId = GenerateKId();

					transaction = Add(key, GetAcceptedRequestIndex(reader), transactionId);
				}

				int action = transaction.ProccessTransport(reader.Method == Methods.Ackm);

				UpdateOrRemove(transaction);

				return action;
			}
		}

		public int ProccessTransportError(int transactionId, out T transaction)
		{
			int action = 0;

			lock (sync)
			{
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTransportError();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		public int ProccessTransactionUser(int transactionId, ServerAsyncEventArgs e, int statusCode)
		{
			int action = 0;

			lock (sync)
			{
				T transaction;
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTransactionUser(statusCode, e);

					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		int IRelaytedTransactionId.GetRelaytedTransactionId(BadServerTransactionKey key)
		{
			lock (sync)
			{
				T transaction;
				if (TryGet(key, out transaction))
					return transaction.Id;
			}

			return Transaction.InvalidKId;
		}

		#region ProccessTimer J G I H (...)

		public int ProccessTimerJ(int transactionId)
		{
			int action = 0;

			lock (sync)
			{
				T transaction;
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerJ();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		public int ProccessTimerG(int transactionId)
		{
			int action = 0;

			lock (sync)
			{
				T transaction;
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerG();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		public int ProccessTimerI(int transactionId)
		{
			int action = 0;

			lock (sync)
			{
				T transaction;
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerI();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		public int ProccessTimerH(int transactionId)
		{
			int action = 0;

			lock (sync)
			{
				T transaction;
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerH();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		#endregion

		#region TryGet, Add, UpdateOrRemove

		private bool TryGet(int transactionId, out T transaction)
		{
			return transactions.TryGetValue(transactionId, out transaction);
		}

		private bool TryGet(BadServerTransactionKey key, out T transaction)
		{
			bool result;
			int index;

			if (transactionIndexes.TryGetValue(key, out index))
			{
				if (transactions.TryGetValue(index, out transaction) == false)
					throw new InvalidProgramException(@"Transaction Index was not removed!");
				result = true;
			}
			else
			{
				transaction = new T();
				result = false;
			}

			return result;
		}

		private int GenerateKId()
		{
			return Transaction.GetTransactionKId(trasactionKind, ++transactionCount);
		}

		private T Add(BadServerTransactionKey key, int router, int transactionKId)
		{
			T transaction;

			key.TransactionId = transactionKId;
			transactionIndexes.Add(key, transactionKId);

			transaction = new T();
			transaction.Initialize(transactionKId, router, key.GetHashCode());

			transactions.Add(transactionKId, transaction);

			return transaction;
		}

		private void UpdateOrRemove(T transaction)
		{
			if (transaction.State == Transaction.States.Terminated)
			{
				transactions.Remove(transaction.Id);

				var key = new BadServerTransactionKey(transaction.Id, transaction.TransactionKeyHashCode);
				transactionIndexes.Remove(key);

				transaction.Dispose();
			}
			else
			{
				transactions[transaction.Id] = transaction;
			}
		}

		#endregion
	}
}

using System;
using System.Threading;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;
using SocketServers;

namespace Sip.Server
{
	sealed class ClientTransactionStorage<T>
		where T : IClientTransaction, new()
	{
		private readonly object sync;
		private readonly Transaction.Kind trasactionKind;
		private int transactionCount;
		private Dictionary<int, T> transactions;

		public ClientTransactionStorage(int capacity, Transaction.Kind kind)
		{
			sync = new object();
			trasactionKind = kind;
			transactions = new Dictionary<int, T>(capacity);
		}

		public int ProccessTransport(SipMessageReader reader, out T transaction)
		{
			int action = 0;
			int transactionKId;

			if (TryDecodeTransactionKId(reader.Via[0].Branch, out transactionKId))
			{
				if (reader.CSeq.Method == Methods.Cancelm)
					transactionKId = Transaction.ChangeKind(transactionKId, Transaction.Kind.CancelClient);

				lock (sync)
				{
					if (TryGet(transactionKId, out transaction))
					{
						action = transaction.ProccessTransport(reader.StatusCode.Value);
						UpdateOrRemove(transaction);
					}
					else
					{
						action |= Transaction.Action.TransactionNotFound;
					}
				}
			}
			else
			{
				transaction = new T();

				if (reader.Via[0].Branch.Equals(SipMessageWriter.C.z9hG4bK_NO_TRANSACTION) == false)
					action |= Transaction.Action.TransactionNotFound;
			}

			return action;
		}

		private bool TryDecodeTransactionKId(ByteArrayPart branch, out int transactionKId)
		{
			int begin = branch.Begin + SipMessage.MagicCookie.Length;
			int end = branch.End;

			if (begin + 8 == end && HexEncoding.TryParseHex8(branch.Bytes, begin, out transactionKId)
				&& Transaction.IsValidTransactionId(transactionKId))
			{
				transactionKId = Transaction.GetTransactionKId(trasactionKind, transactionKId);
				return true;
			}

			transactionKId = 0;
			return false;
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

		public int ProccessTransactionUser(int transactionId, int router, int userData, bool isAck, ServerAsyncEventArgs e)
		{
			lock (sync)
			{
				var transaction = Add(transactionId, router, userData);

				int action = transaction.ProccessTransactionUser(isAck, e);

				UpdateOrRemove(transaction);

				return action;
			}
		}

		public int GetTransactionId()
		{
			return Transaction.GetTransactionKId(trasactionKind, Interlocked.Increment(ref transactionCount));
		}

		#region ProccessTimer E F K A B D (...)

		public int ProccessTimerE(int transactionId)
		{
			int action = 0;

			lock (sync)
			{
				T transaction;
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerE();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		public int ProccessTimerF(int transactionId, out T transaction)
		{
			int action = 0;

			lock (sync)
			{
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerF();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		public int ProccessTimerK(int transactionId)
		{
			int action = 0;

			lock (sync)
			{
				T transaction;
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerK();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		public int ProccessTimerA(int transactionId)
		{
			int action = 0;

			lock (sync)
			{
				T transaction;
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerA();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		public int ProccessTimerB(int transactionId, out T transaction)
		{
			int action = 0;

			lock (sync)
			{
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerB();
					UpdateOrRemove(transaction);
				}
			}

			return action;
		}

		public int ProccessTimerD(int transactionId)
		{
			int action = 0;

			lock (sync)
			{
				T transaction;
				if (TryGet(transactionId, out transaction))
				{
					action = transaction.ProccessTimerD();
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

		private T Add(int id, int router, int userData)
		{
			T transaction = new T();
			transaction.Initialize(id, router, userData);

			transactions.Add(id, transaction);

			return transaction;
		}

		private void UpdateOrRemove(T transaction)
		{
			if (transaction.State == Transaction.States.Terminated)
			{
				transactions.Remove(transaction.Id);
				transaction.Dispose();
			}
			else
				transactions[transaction.Id] = transaction;
		}

		#endregion
	}
}

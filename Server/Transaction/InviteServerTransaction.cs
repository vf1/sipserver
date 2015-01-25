using System;
using Sip.Tools;
using SocketServers;

namespace Sip.Server
{
	struct InviteServerTransaction
		: IServerTransaction
	{
		private int selectTimerG;

		public int Id { get; private set; }
		public int Router { get; private set; }
		public int UserData { get { return 0; } }
		public Transaction.States State { get; private set; }
		public Transaction.Kind Kind { get { return Transaction.Kind.InviteServer; } }

		public void Initialize(int id, int router, int transactionKeyHashCode)
		{
			Id = id;
			Router = router;
			TransactionKeyHashCode = transactionKeyHashCode;
		}

		public int TransactionKeyHashCode { get; private set; }

		private ServerAsyncEventArgs eventArgs;

		public void Dispose()
		{
			if (eventArgs != null)
				eventArgs.Dispose();
		}

		public bool IsTransportUnreliable
		{
			get { return eventArgs != null && eventArgs.LocalEndPoint.IsTransportUnreliable(); }
		}

		public int ProccessTransport(bool isAck)
		{
			int action = 0;

			switch (State)
			{
				case Transaction.States.Created:
					action |= Transaction.Action.PassIncomingRequest;
					action |= SetState(Transaction.States.Proceeding);
					break;

				case Transaction.States.Proceeding:
					action |= Transaction.Action.SendCachedMessage;
					break;

				case Transaction.States.Completed:
					if (isAck)
					{
						if (IsTransportUnreliable)
							action |= SetState(Transaction.States.Confirmed);
						else
							action |= SetState(Transaction.States.Terminated);
					}
					else
					{
						action |= Transaction.Action.SendCachedMessage;
					}
					break;

				case Transaction.States.Confirmed:
					break;

				case Transaction.States.Terminated:
					break;
			}

			return action;
		}

		public int ProccessTransportError()
		{
			int action = 0;

			switch (State)
			{
				case Transaction.States.Proceeding:
				case Transaction.States.Completed:
					action |= Transaction.Action.InformTuAboutError;
					action |= SetState(Transaction.States.Terminated);
					break;

				case Transaction.States.Confirmed:
					break;

				case Transaction.States.Terminated:
					break;
			}

			return action;
		}

		public int ProccessTransactionUser(int statusCode, ServerAsyncEventArgs e)
		{
			int action = 0;

			switch (State)
			{
				case Transaction.States.Proceeding:

					action |= Transaction.Action.SendOutgoingMessage;

					if (statusCode >= 101 && statusCode <= 199)
					{
						CacheMessage(e);
					}
					else if (statusCode >= 200 && statusCode <= 299)
					{
						action |= SetState(Transaction.States.Terminated);
					}
					else if (statusCode >= 300 && statusCode <= 699)
					{
						CacheMessage(e);
						action |= SetState(Transaction.States.Completed);
					}
					break;


				case Transaction.States.Completed:
				case Transaction.States.Confirmed:
				case Transaction.States.Terminated:
					break;
			}

			return action;
		}

		public int ProccessTimerG()
		{
			int action = 0;

			if (State == Transaction.States.Completed && IsTransportUnreliable)
			{
				action = Transaction.Action.SendCachedMessage;

				switch (++selectTimerG)
				{
					case 1:
						action |= Transaction.Action.StartTimerG2;
						break;
					case 2:
						action |= Transaction.Action.StartTimerG3;
						break;
					default:
						action |= Transaction.Action.StartTimerG4;
						break;
				}
			}

			return action;
		}

		public int ProccessTimerH()
		{
			return SetState(Transaction.States.Terminated);
		}

		public int ProccessTimerI()
		{
			return SetState(Transaction.States.Terminated);
		}

		public int ProccessTimerJ()
		{
			return 0;
		}

		public ServerAsyncEventArgs GetCachedCopy()
		{
			return (eventArgs == null) ? null : eventArgs.CreateDeepCopy();
		}

		private int SetState(Transaction.States newState)
		{
			int action = 0;

			if (State != newState)
			{
				if (newState == Transaction.States.Completed)
				{
					action |= Transaction.Action.StartTimerH;

					if (IsTransportUnreliable)
						action |= Transaction.Action.StartTimerG1;
				}

				if (newState == Transaction.States.Confirmed)
					action |= Transaction.Action.StartTimerI;

				State = newState;
			}

			return action;
		}

		private void CacheMessage(ServerAsyncEventArgs e)
		{
			if (eventArgs != null)
				eventArgs.Dispose();
			eventArgs = e.CreateDeepCopy();
		}
	}
}

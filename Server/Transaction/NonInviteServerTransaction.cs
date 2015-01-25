using System;
using Sip.Tools;
using SocketServers;

namespace Sip.Server
{
	struct NonInviteServerTransaction
		: IServerTransaction
	{
		public int Id { get; private set; }
		public int Router { get; private set; }
		public int UserData { get { return 0; } }
		public Transaction.States State { get; private set; }
		public Transaction.Kind Kind { get { return Transaction.Kind.NonInviteServer; } }

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

		public int ProccessTransport(bool isAck_NotUsed)
		{
			int action = 0;

			switch (State)
			{
				case Transaction.States.Created:
					action |= Transaction.Action.PassIncomingRequest;
					SetState(Transaction.States.Trying, ref action);
					break;

				case Transaction.States.Trying:
					break;

				case Transaction.States.Proceeding:
				case Transaction.States.Completed:
					action |= Transaction.Action.SendCachedMessage;
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
				case Transaction.States.Trying:
					break;

				case Transaction.States.Proceeding:
				case Transaction.States.Completed:
					action |= Transaction.Action.InformTuAboutError;
					SetState(Transaction.States.Terminated, ref action);
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
				case Transaction.States.Trying:

					action |= Transaction.Action.SendOutgoingMessage;

					if (statusCode >= 100 && statusCode <= 199)
					{
						eventArgs = e.CreateDeepCopy();
						SetState(Transaction.States.Proceeding, ref action);
					}
					else if (statusCode >= 200 && statusCode <= 699)
					{
						if (e.LocalEndPoint.IsTransportUnreliable())
							SetState(Transaction.States.Completed, ref action);
						else
							SetState(Transaction.States.Terminated, ref action);
					}

					break;


				case Transaction.States.Proceeding:

					action |= Transaction.Action.SendOutgoingMessage;

					if (statusCode >= 200 && statusCode <= 699)
					{
						if (e.LocalEndPoint.IsTransportUnreliable())
						{
							eventArgs = e.CreateDeepCopy();
							SetState(Transaction.States.Completed, ref action);
						}
						else
						{
							SetState(Transaction.States.Terminated, ref action);
						}
					}
					break;


				case Transaction.States.Completed:
				case Transaction.States.Terminated:
					break;
			}

			return action;
		}

		public ServerAsyncEventArgs GetCachedCopy()
		{
			return (eventArgs == null) ? null : eventArgs.CreateDeepCopy();
		}

		public int ProccessTimerJ()
		{
			int action = 0;
			SetState(Transaction.States.Terminated, ref action);
			return action;
		}

		public int ProccessTimerG()
		{
			return 0;
		}

		public int ProccessTimerI()
		{
			return 0;
		}

		public int ProccessTimerH()
		{
			return 0;
		}

		private void SetState(Transaction.States newState, ref int action)
		{
			if (State != newState)
			{
				if (newState == Transaction.States.Completed)
					action |= Transaction.Action.StartTimerJ;

				State = newState;
			}
		}
	}
}

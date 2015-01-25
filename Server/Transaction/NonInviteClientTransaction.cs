using System;
using System.Net;
using Sip.Tools;
using SocketServers;

namespace Sip.Server
{
	struct NonInviteClientTransaction
		: IClientTransaction
	{
		private int selectTimerE;

		public int Id { get; private set; }
		public int Router { get; private set; }
		public int UserData { get; private set; }
		public Transaction.States State { get; private set; }
		public Transaction.Kind Kind { get { return Transaction.Kind.NonInviteClient; } }

		public void Initialize(int id, int router, int userData)
		{
			Id = id;
			Router = router;
			UserData = userData;
		}

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

		public int ProccessTransport(int statusCode)
		{
			int action = 0;

			switch (State)
			{
				case Transaction.States.Trying:
					action |= Transaction.Action.PassIncomingResponse;
					if (statusCode <= 199)
					{
						action |= SetState(Transaction.States.Proceeding);
					}
					else
					{
						if (IsTransportUnreliable)
							action |= SetState(Transaction.States.Completed);
						else
							action |= SetState(Transaction.States.Terminated);
					}
					break;


				case Transaction.States.Proceeding:
					action |= Transaction.Action.PassIncomingResponse;
					if (statusCode >= 200)
					{
						if (IsTransportUnreliable)
							action |= SetState(Transaction.States.Completed);
						else
							action |= SetState(Transaction.States.Terminated);
					}
					break;

				case Transaction.States.Completed:
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
				case Transaction.States.Proceeding:
					action |= Transaction.Action.InformTuAboutError;
					action |= SetState(Transaction.States.Terminated);
					break;

				case Transaction.States.Completed:
				case Transaction.States.Terminated:
					break;
			}

			return action;
		}

		public int ProccessTransactionUser(bool isAck, ServerAsyncEventArgs e)
		{
			int action = 0;

			switch (State)
			{
				case Transaction.States.Created:

					action |= Transaction.Action.SendOutgoingMessage;

					if (e.LocalEndPoint.IsTransportUnreliable())
						eventArgs = e.CreateDeepCopy();

					action |= SetState(Transaction.States.Trying);
					break;

				case Transaction.States.Trying:
				case Transaction.States.Proceeding:
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

		public int ProccessTimerE()
		{
			int action = 0;

			if (State == Transaction.States.Trying && IsTransportUnreliable)
			{
				action = Transaction.Action.SendCachedMessage;

				switch (++selectTimerE)
				{
					case 1:
						action |= Transaction.Action.StartTimerE2;
						break;
					case 2:
						action |= Transaction.Action.StartTimerE3;
						break;
					default:
						action |= Transaction.Action.StartTimerE4;
						break;
				}
			}

			return action;
		}

		public int ProccessTimerF()
		{
			int action = 0;

			if (State == Transaction.States.Trying || State == Transaction.States.Proceeding)
				action = SetState(Transaction.States.Terminated) | Transaction.Action.InformTuAboutError;

			return action;
		}

		public int ProccessTimerK()
		{
			int action = 0;

			if (State == Transaction.States.Completed)
				action = SetState(Transaction.States.Terminated);

			return action;
		}

		public int ProccessTimerA()
		{
			return 0;
		}

		public int ProccessTimerB()
		{
			return 0;
		}

		public int ProccessTimerD()
		{
			return 0;
		}

		private int SetState(Transaction.States newState)
		{
			int action = 0;

			if (State != newState)
			{
				if (newState == Transaction.States.Trying)
				{
					if (IsTransportUnreliable)
						action |= Transaction.Action.StartTimerE1;

					action |= Transaction.Action.StartTimerF;
				}
				else if (newState == Transaction.States.Completed)
				{
					if (IsTransportUnreliable)
						action |= Transaction.Action.StartTimerK;
				}

				State = newState;
			}

			return action;
		}
	}
}

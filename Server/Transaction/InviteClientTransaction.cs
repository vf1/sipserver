using System;
using System.Net;
using Sip.Tools;
using SocketServers;

namespace Sip.Server
{
	struct InviteClientTransaction
		: IClientTransaction
	{
		private int selectTimerA;
		private bool isAckCached;
		private ServerAsyncEventArgs eventArgs;

		public int Id { get; private set; }
		public int Router { get; private set; }
		public int UserData { get; private set; }
		public Transaction.States State { get; private set; }
		public Transaction.Kind Kind { get { return Transaction.Kind.InviteClient; } }

		public void Initialize(int id, int router, int userData)
		{
			Id = id;
			Router = router;
			UserData = userData;
		}

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
				case Transaction.States.Calling:
					action |= Transaction.Action.PassIncomingResponse;
					if (statusCode <= 199)
					{
						action |= SetState(Transaction.States.Proceeding);
					}
					else if (statusCode <= 299)
					{
						action |= SetState(Transaction.States.Terminated);
					}
					else if (statusCode <= 699)
					{
						action |= Transaction.Action.SendAck;
						if (IsTransportUnreliable)
							action |= SetState(Transaction.States.Completed);
						else
							action |= SetState(Transaction.States.Terminated);
					}
					break;


				case Transaction.States.Proceeding:
					action |= Transaction.Action.PassIncomingResponse;
					if (statusCode >= 200 && statusCode <= 299)
					{
						action |= SetState(Transaction.States.Terminated);
					}
					else if (statusCode >= 300 && statusCode <= 699)
					{
						action |= Transaction.Action.SendAck;
						if (IsTransportUnreliable)
							action |= SetState(Transaction.States.Completed);
						else
							action |= SetState(Transaction.States.Terminated);
					}
					break;

				case Transaction.States.Completed:
					if (statusCode >= 300 && statusCode <= 699)
					{
						if (isAckCached)
							action |= Transaction.Action.SendCachedMessage;
						else
							action |= Transaction.Action.SendAck;
					}
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
				case Transaction.States.Calling:
					action |= Transaction.Action.InformTuAboutError;
					action |= SetState(Transaction.States.Terminated);
					break;

				case Transaction.States.Proceeding:
					break;

				case Transaction.States.Completed:
					goto case Transaction.States.Calling;

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

					eventArgs = e.CreateDeepCopy();
					action |= SetState(Transaction.States.Calling);
					break;

				case Transaction.States.Calling:
				case Transaction.States.Proceeding:
					break;

				case Transaction.States.Completed:
					if (isAck && IsTransportUnreliable)
					{
						if (eventArgs != null)
							eventArgs.Dispose();
						eventArgs = e.CreateDeepCopy();
						isAckCached = true;
					}
					break;

				case Transaction.States.Terminated:
					break;
			}

			return action;
		}

		public ServerAsyncEventArgs GetCachedCopy()
		{
			return (eventArgs == null) ? null : eventArgs.CreateDeepCopy();
		}

		public int ProccessTimerA()
		{
			int action = 0;

			if (State == Transaction.States.Calling && IsTransportUnreliable)
			{
				action = Transaction.Action.SendCachedMessage;

				switch (++selectTimerA)
				{
					case 1:
						action |= Transaction.Action.StartTimerA2;
						break;
					case 2:
						action |= Transaction.Action.StartTimerA3;
						break;
					default:
						action |= Transaction.Action.StartTimerA4;
						break;
				}
			}

			return action;
		}

		public int ProccessTimerB()
		{
			int action = 0;

			if (State == Transaction.States.Calling)
				action = SetState(Transaction.States.Terminated);

			return action;
		}

		public int ProccessTimerD()
		{
			int action = 0;

			if (State == Transaction.States.Completed)
				action = SetState(Transaction.States.Terminated);

			return action;
		}

		public int ProccessTimerE()
		{
			return 0;
		}

		public int ProccessTimerF()
		{
			return 0;
		}

		public int ProccessTimerK()
		{
			return 0;
		}

		private int SetState(Transaction.States newState)
		{
			int action = 0;

			if (State != newState)
			{
				if (newState == Transaction.States.Calling)
				{
					if (IsTransportUnreliable)
						action |= Transaction.Action.StartTimerA1;

					action |= Transaction.Action.StartTimerB;
				}
				else if (newState == Transaction.States.Completed)
				{
					if (IsTransportUnreliable)
						action |= Transaction.Action.StartTimerD;
				}

				State = newState;
			}

			return action;
		}
	}
}

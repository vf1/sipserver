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
	class NonInviteServerTransactionTest
		: BaseServerTransactionTest<NonInviteServerTransaction>
	{
		[Test]
		public void n1_It_should_have_initial_state_Created()
		{
			var transaction = new InviteServerTransaction();
			Assert.AreEqual(Transaction.States.Created, transaction.State);
		}

		[Test]
		public void n2_It_should_correct_move_from_Trying_state_by_TU_event()
		{
			EmulateTuEvent(false, Transaction.States.Trying, 200, 699, Transaction.States.Terminated);
			EmulateTuEvent(true, Transaction.States.Trying, 200, 699, Transaction.States.Completed);
			EmulateTuEvent(Transaction.States.Trying, 100, 199, Transaction.States.Proceeding);
		}

		[Test]
		public void n3_It_should_correct_move_from_Proceeding_state_by_TU_event()
		{
			EmulateTuEvent(false, Transaction.States.Proceeding, 200, 699, Transaction.States.Terminated);
			EmulateTuEvent(true, Transaction.States.Proceeding, 200, 699, Transaction.States.Completed);
			EmulateTuEvent(Transaction.States.Proceeding, 100, 199, Transaction.States.Proceeding);
		}

		[Test]
		public void n4_It_should_go_to_Terminated_by_Transport_error_from_Porceeding_and_Completed()
		{
			foreach (var transaction in GetAllTransactionsFor(Transaction.States.Proceeding, Transaction.States.Completed))
			{
				int action = transaction.ProccessTransportError();
				Assert.AreEqual(Transaction.Action.InformTuAboutError, action);
				Assert.AreEqual(Transaction.States.Terminated, transaction.State);
			}
		}

		[Test]
		public void n5_It_should_NOT_go_to_Terminated_by_Transport_error_from_Trying()
		{
			foreach (var transaction in GetAllTransactionsFor(Transaction.States.Confirmed))
			{
				Assert.AreEqual(0, transaction.ProccessTransportError());
				Assert.AreEqual(Transaction.States.Confirmed, transaction.State);
			}
		}

		[Test]
		public void n7_It_should_stay_in_Procceeding_by_INVITE()
		{
			foreach (var transaction in GetAllTransactionsFor(Transaction.States.Proceeding, Transaction.States.Completed))
			{
				var oldState = transaction.State;
				int action = transaction.ProccessTransport(false);
				Assert.AreEqual(Transaction.Action.SendCachedMessage, action);
				Assert.AreEqual(oldState, transaction.State);
			}
		}

		[Test]
		public void n8_It_should_start_timer_J_for_Completed_for_unreliable()
		{
			foreach (var state in GetAllStatesFor(Transaction.States.Trying, Transaction.States.Proceeding))
			{
				if (state.IsTransportUnreliable)
				{
					var e = GetServerEventArgs(state.IsTransportUnreliable);
					var transaction = GetTransaction(state.State, state.IsTransportUnreliable);

					int action = transaction.ProccessTransactionUser(200, e);
					Assert.AreEqual(Transaction.Action.StartTimerJ, action & Transaction.Action.StartTimerJ);

					EventArgsManager.Put(e);
				}
			}
		}

		[Test]
		public void nB_It_should_goto_to_Terminated_by_timer_J()
		{
			var unreliable = GetTransaction(Transaction.States.Completed, true);
			unreliable.ProccessTimerJ();

			Assert.AreEqual(Transaction.States.Terminated, unreliable.State);
		}

		[Test]
		public void nC_It_should_send_outgoing_response()
		{
			for (int i = 100; i <= 699; i++)
				foreach (var state in GetAllStatesFor(Transaction.States.Trying))
				{
					var e = GetServerEventArgs(state.IsTransportUnreliable);
					var transaction = GetTransaction(state.State, state.IsTransportUnreliable);

					int action = transaction.ProccessTransactionUser(i, e);
					Assert.AreEqual(Transaction.Action.SendOutgoingMessage, action & Transaction.Action.SendOutgoingMessage);

					EventArgsManager.Put(e);
				}
		}

		[Test]
		public void nD_It_should_NOT_send_response()
		{
			for (int i = 100; i <= 699; i++)
				foreach (var state in GetAllStatesExcept(Transaction.States.Trying, Transaction.States.Proceeding))
				{
					var e = GetServerEventArgs(state.IsTransportUnreliable);
					var transaction = GetTransaction(state.State, state.IsTransportUnreliable);

					int action = transaction.ProccessTransactionUser(i, e);
					Assert.AreEqual(Transaction.Action.SendOutgoingMessage, action & Transaction.Action.SendOutgoingMessage);

					EventArgsManager.Put(e);
				}
		}

		protected override NonInviteServerTransaction GetTransaction(Transaction.States state, bool isTransportUnreliable)
		{
			var e = GetServerEventArgs(isTransportUnreliable);

			var transaction = new NonInviteServerTransaction();

			switch (state)
			{
				case Transaction.States.Created:
					break;

				case Transaction.States.Trying:
					transaction.ProccessTransport(false);
					break;

				case Transaction.States.Proceeding:
					transaction.ProccessTransport(false);
					transaction.ProccessTransactionUser(100, e);
					break;

				case Transaction.States.Completed:
					if (isTransportUnreliable == false)
						throw new InvalidProgramException(@"States.Completed state accessable only for Unreable transport");
					transaction.ProccessTransport(false);
					transaction.ProccessTransactionUser(200, e);
					break;

				case Transaction.States.Terminated:
					transaction.ProccessTransport(false);
					transaction.ProccessTransportError();
					break;
			}

			if (state != transaction.State)
				throw new InvalidProgramException("GetTransaction can not goto desired state");

			EventArgsManager.Put(e);

			return transaction;
		}

		protected override IEnumerable<StateUnreliable> GetAllStates()
		{
			return new StateUnreliable[]
			{
				new StateUnreliable(Transaction.States.Trying, true),
				new StateUnreliable(Transaction.States.Trying, false),
				new StateUnreliable(Transaction.States.Proceeding, true),
				new StateUnreliable(Transaction.States.Proceeding, false),
				new StateUnreliable(Transaction.States.Completed, true),
				new StateUnreliable(Transaction.States.Terminated, true),
				new StateUnreliable(Transaction.States.Terminated, false),
			};
		}
	}
}

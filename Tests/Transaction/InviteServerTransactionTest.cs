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
	class InviteServerTransactionTest
		: BaseServerTransactionTest<InviteServerTransaction>
	{
		[Test]
		public void n1_It_should_have_initial_state_Created()
		{
			var transaction = new InviteServerTransaction();
			Assert.AreEqual(Transaction.States.Created, transaction.State);
		}

		[Test]
		public void n2_It_should_correct_move_from_Proceeding_state_by_TU_event()
		{
			EmulateTuEvent(Transaction.States.Proceeding, 300, 699, Transaction.States.Completed);
			EmulateTuEvent(Transaction.States.Proceeding, 200, 299, Transaction.States.Terminated);
			EmulateTuEvent(Transaction.States.Proceeding, 100, 199, Transaction.States.Proceeding);
		}

		[Test]
		public void n3_It_should_go_to_Terminated_by_Transport_error_from_Porceeding_and_Completed()
		{
			foreach (var transaction in GetAllTransactionsFor(Transaction.States.Proceeding, Transaction.States.Completed))
			{
				int action = transaction.ProccessTransportError();
				Assert.AreEqual(Transaction.Action.InformTuAboutError, action);
				Assert.AreEqual(Transaction.States.Terminated, transaction.State);
			}
		}

		[Test]
		public void n4_It_should_NOT_go_to_Terminated_by_Transport_error_from_Confirmed()
		{
			foreach (var transaction in GetAllTransactionsFor(Transaction.States.Confirmed))
			{
				Assert.AreEqual(0, transaction.ProccessTransportError());
				Assert.AreEqual(Transaction.States.Confirmed, transaction.State);
			}
		}

		[Test]
		public void n5_It_should_go_to_Terminated_by_ACK_on_Reliable_Transport_from_Completed()
		{
			var transaction = GetTransaction(Transaction.States.Completed, false);
			int action = transaction.ProccessTransport(true);
			Assert.AreEqual(0, action);
			Assert.AreEqual(Transaction.States.Terminated, transaction.State);
		}

		[Test]
		public void n6_It_should_go_to_Confirmed_by_ACK_on_Unreliable_Transport_from_Completed()
		{
			var transaction = GetTransaction(Transaction.States.Completed, true);
			int action = transaction.ProccessTransport(true);
			Assert.AreEqual(Transaction.Action.StartTimerI, action);
			Assert.AreEqual(Transaction.States.Confirmed, transaction.State);
		}

		[Test]
		public void n7_It_should_stay_in_Procceeding_by_INVITE()
		{
			foreach (var transaction in GetAllTransactionsFor(Transaction.States.Proceeding))
			{
				int action = transaction.ProccessTransport(false);
				Assert.AreEqual(Transaction.Action.SendCachedMessage, action);
				Assert.AreEqual(Transaction.States.Proceeding, transaction.State);
			}
		}

		[Test]
		public void n8_It_should_start_timer_H_for_Completed_and_timer_G_for_unreliable()
		{
			foreach (var state in GetAllStatesFor(Transaction.States.Proceeding))
			{
				var e = GetServerEventArgs(state.IsTransportUnreliable);
				var transaction = GetTransaction(state.State, state.IsTransportUnreliable);

				int action = transaction.ProccessTransactionUser(300, e);
				Assert.AreEqual(Transaction.Action.StartTimerH, action & Transaction.Action.StartTimerH);
				Assert.AreEqual(state.IsTransportUnreliable ? Transaction.Action.StartTimerG1 : 0, action & Transaction.Action.StartTimerG1);

				EventArgsManager.Put(e);
			}
		}

		[Test]
		public void n9_It_should_request_timer_G1_then_G2_G3_G4_G4_G4_and_etc_in_Completed_state()
		{
			var expectedActions = new int[] {
					Transaction.Action.StartTimerG2,
					Transaction.Action.StartTimerG3,
					Transaction.Action.StartTimerG4,
					Transaction.Action.StartTimerG4,
					Transaction.Action.StartTimerG4,
					Transaction.Action.StartTimerG4,
					Transaction.Action.StartTimerG4,
			};

			var transaction = GetTransaction(Transaction.States.Completed, true);

			foreach (var expectedAction in expectedActions)
				Assert.AreEqual(Transaction.Action.SendCachedMessage | expectedAction, transaction.ProccessTimerG());
		}

		[Test]
		public void nA_It_should_NOT_request_timer_G_for_some_cases()
		{
			var reliable = GetTransaction(Transaction.States.Completed, false);

			for (int i = 0; i < 100; i++)
				Assert.AreEqual(0, reliable.ProccessTimerG());

			foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Completed))
				Assert.AreEqual(0, transaction.ProccessTimerG());
		}

		[Test]
		public void nB_It_should_goto_to_Terminated_by_timer_I()
		{
			var unreliable = GetTransaction(Transaction.States.Completed, true);
			unreliable.ProccessTimerI();

			Assert.AreEqual(Transaction.States.Terminated, unreliable.State);
		}

		[Test]
		public void nC_It_should_send_outgoing_response()
		{
			for (int i = 100; i <= 699; i++)
				foreach (var state in GetAllStatesFor(Transaction.States.Proceeding))
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
				foreach (var state in GetAllStatesExcept(Transaction.States.Proceeding))
				{
					var e = GetServerEventArgs(state.IsTransportUnreliable);
					var transaction = GetTransaction(state.State, state.IsTransportUnreliable);

					int action = transaction.ProccessTransactionUser(i, e);
					Assert.AreEqual(Transaction.Action.SendOutgoingMessage, action & Transaction.Action.SendOutgoingMessage);

					EventArgsManager.Put(e);
				}
		}

		protected override InviteServerTransaction GetTransaction(Transaction.States state, bool isTransportUnreliable)
		{
			var e = GetServerEventArgs(isTransportUnreliable);

			var transaction = new InviteServerTransaction();

			switch (state)
			{
				case Transaction.States.Created:
					break;

				case Transaction.States.Proceeding:
					transaction.ProccessTransport(false);
					break;

				case Transaction.States.Completed:
					transaction.ProccessTransport(false);
					transaction.ProccessTransactionUser(300, e);
					break;

				case Transaction.States.Confirmed:
					if (isTransportUnreliable == false)
						throw new InvalidProgramException(@"States.Completed state accessable only for Unreable transport");
					transaction.ProccessTransport(false);
					transaction.ProccessTransactionUser(300, e);
					transaction.ProccessTransport(true);
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
				new StateUnreliable(Transaction.States.Proceeding, true),
				new StateUnreliable(Transaction.States.Proceeding, false),
				new StateUnreliable(Transaction.States.Completed, true),
				new StateUnreliable(Transaction.States.Completed, false),
				new StateUnreliable(Transaction.States.Confirmed, true),
				new StateUnreliable(Transaction.States.Terminated, true),
				new StateUnreliable(Transaction.States.Terminated, false),
			};
		}
	}
}

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
	class NonInviteClientTransactionTest
		: BaseClientTransactionTest<NonInviteClientTransaction>
	{
		[Test]
		public void n1_It_should_have_initial_state_Created()
		{
			var transaction = new NonInviteClientTransaction();
			Assert.AreEqual(Sip.Server.Transaction.States.Created, transaction.State);
		}

		[Test]
		public void n2_It_should_correct_move_from_Trying_by_transport_event()
		{
			EmulateTransportEvent(Transaction.States.Trying, 100, 199, Transaction.States.Proceeding);

			EmulateTransportEvent(false, Transaction.States.Trying, 200, 699, Transaction.States.Terminated);
			EmulateTransportEvent(true, Transaction.States.Trying, 200, 699, Transaction.States.Completed);
		}

		[Test]
		public void n3_It_should_correct_move_from_Proceeding_by_transport_event()
		{
			EmulateTransportEvent(Transaction.States.Proceeding, 100, 199, Transaction.States.Proceeding);

			EmulateTransportEvent(false, Transaction.States.Proceeding, 200, 699, Transaction.States.Terminated);
			EmulateTransportEvent(true, Transaction.States.Proceeding, 200, 699, Transaction.States.Completed);
		}

		[Test]
		public void n5_It_should_from_almost_all_states_goto_to_Terminated_by_transport_error()
		{
			foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Completed, Transaction.States.Terminated))
			{
				int action = transaction.ProccessTransportError();
				Assert.AreEqual(Transaction.States.Terminated, transaction.State);
				Assert.AreEqual(Transaction.Action.InformTuAboutError, action);
			}

			{
				var transaction = GetTransaction(Transaction.States.Completed, true);
				int action = transaction.ProccessTransportError();
				Assert.AreEqual(Transaction.States.Completed, transaction.State);
				Assert.AreEqual(0, action);
			}
		}

		[Test]
		public void n6_It_should_request_timer_E_for_unreliable_transport_and_timer_F_for_any()
		{
			int timers = Transaction.Action.StartTimerF | Transaction.Action.StartTimerE1;

			var reliable = new NonInviteClientTransaction();
			var e1 = GetServerEventArgs(false);
			int action1 = reliable.ProccessTransactionUser(false, e1);
			Assert.AreEqual(Transaction.Action.StartTimerF, action1 & timers);
			EventArgsManager.Put(ref e1);

			var unreliable = new NonInviteClientTransaction();
			var e2 = GetServerEventArgs(true);
			int action2 = unreliable.ProccessTransactionUser(false, e2);
			Assert.AreEqual(timers, action2 & timers);
			EventArgsManager.Put(ref e2);
		}

		[Test]
		public void n7_It_should_request_timer_E1_then_E2_E3_E4_E4_E4_and_etc_in_Trying_state()
		{
			var expectedActions = new int[] {
					Transaction.Action.StartTimerE2,
					Transaction.Action.StartTimerE3,
					Transaction.Action.StartTimerE4,
					Transaction.Action.StartTimerE4,
					Transaction.Action.StartTimerE4,
					Transaction.Action.StartTimerE4,
					Transaction.Action.StartTimerE4,
			};

			var transaction = GetTransaction(Transaction.States.Calling, true);

			foreach (var expectedAction in expectedActions)
				Assert.AreEqual(Transaction.Action.SendCachedMessage | expectedAction, transaction.ProccessTimerE());
		}

		[Test]
		public void n8_It_should_NOT_request_timer_E_for_some_cases()
		{
			var reliable = GetTransaction(Transaction.States.Calling, false);

			for (int i = 0; i < 100; i++)
				Assert.AreEqual(0, reliable.ProccessTimerE());

			foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Calling))
				Assert.AreEqual(0, transaction.ProccessTimerE());
		}

		[Test]
		public void n9_It_should_goto_to_Terminated_by_timer_F_from_Trying_only()
		{
			foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Completed, Transaction.States.Terminated))
			{
				var oldState = transaction.State;
				Assert.AreEqual(Transaction.Action.InformTuAboutError, transaction.ProccessTimerF());
				Assert.AreEqual(Transaction.States.Terminated, transaction.State);
			}

			foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Trying, Transaction.States.Proceeding))
			{
				var oldState = transaction.State;
				Assert.AreEqual(0, transaction.ProccessTimerF());
				Assert.AreEqual(oldState, transaction.State);
			}
		}

		[Test]
		public void nA_It_should_request_timer_K_for_unreliable_transport()
		{
			var unreliable = GetTransaction(Transaction.States.Trying, true);
			Assert.AreEqual(Transaction.Action.StartTimerK, unreliable.ProccessTransport(300) & Transaction.Action.StartTimerK);

			var reliable = GetTransaction(Transaction.States.Trying, false);
			Assert.AreEqual(0, reliable.ProccessTransport(300) & Transaction.Action.StartTimerK);
		}

		[Test]
		public void nC_It_should_pass_response_to_TU()
		{
			for (int i = 100; i <= 699; i++)
				foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Completed, Transaction.States.Terminated))
					Assert.AreEqual(Transaction.Action.PassIncomingResponse, transaction.ProccessTransport(i) & Transaction.Action.PassIncomingResponse);
		}

		[Test]
		public void nD_It_should_NOT_pass_response_to_TU()
		{
			for (int i = 100; i <= 699; i++)
				foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Trying, Transaction.States.Proceeding))
					Assert.AreEqual(0, transaction.ProccessTransport(i) & Transaction.Action.PassIncomingResponse);
		}

		[Test]
		public void nE_It_should_goto_to_Terminated_by_timer_K()
		{
			var unreliable = GetTransaction(Transaction.States.Completed, true);
			unreliable.ProccessTimerK();

			Assert.AreEqual(Transaction.States.Terminated, unreliable.State);
		}

		[Test]
		public void nF_It_should_send_initial_Request()
		{
			{
				var e = GetServerEventArgs(false);
				int action = new NonInviteClientTransaction().ProccessTransactionUser(false, e);
				Assert.AreEqual(Transaction.Action.SendOutgoingMessage, action & Transaction.Action.SendOutgoingMessage);
				EventArgsManager.Put(e);
			}
			{
				var e = GetServerEventArgs(true);
				int action = new NonInviteClientTransaction().ProccessTransactionUser(false, e);
				Assert.AreEqual(Transaction.Action.SendOutgoingMessage, action & Transaction.Action.SendOutgoingMessage);
				EventArgsManager.Put(e);
			}
		}

		protected override IEnumerable<StateUnreliable> GetAllStates()
		{
			return new StateUnreliable[]
			{
				new StateUnreliable(Transaction.States.Trying, false),
				new StateUnreliable(Transaction.States.Trying, true),
				new StateUnreliable(Transaction.States.Proceeding, true),
				new StateUnreliable(Transaction.States.Proceeding, false),
				new StateUnreliable(Transaction.States.Completed, true),
				new StateUnreliable(Transaction.States.Terminated, true),
				new StateUnreliable(Transaction.States.Terminated, false),
			};
		}
	}
}

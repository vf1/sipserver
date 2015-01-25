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
	[TestFixture]
	class InviteClientTransactionTest
		: BaseClientTransactionTest<InviteClientTransaction>
	{
		[Test]
		public void n1_It_should_have_initial_state_Created()
		{
			var transaction = new InviteClientTransaction();
			Assert.AreEqual(Sip.Server.Transaction.States.Created, transaction.State);
		}

		[Test]
		public void n2_It_should_correct_move_from_Calling_by_transport_event()
		{
			EmulateTransportEvent(Transaction.States.Calling, 200, 299, Transaction.States.Terminated);
			EmulateTransportEvent(Transaction.States.Calling, 100, 199, Transaction.States.Proceeding);

			EmulateTransportEvent(false, Transaction.States.Calling, 300, 699, Transaction.States.Terminated);
			EmulateTransportEvent(true, Transaction.States.Calling, 300, 699, Transaction.States.Completed);
		}

		[Test]
		public void n3_It_should_correct_move_from_Proceeding_by_transport_event()
		{
			EmulateTransportEvent(Transaction.States.Proceeding, 200, 299, Transaction.States.Terminated);
			EmulateTransportEvent(Transaction.States.Proceeding, 100, 199, Transaction.States.Proceeding);

			EmulateTransportEvent(false, Transaction.States.Proceeding, 300, 699, Transaction.States.Terminated);
			EmulateTransportEvent(true, Transaction.States.Proceeding, 300, 699, Transaction.States.Completed);
		}

		[Test]
		public void n4_It_should_correct_move_from_Completed_by_transport_event()
		{
			EmulateTransportEvent(true, Transaction.States.Proceeding, 300, 699, Transaction.States.Completed);
		}

		[Test]
		public void n5_It_should_from_almost_all_states_goto_to_Terminated_by_transport_error()
		{
			foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Proceeding, Transaction.States.Terminated))
			{
				int action = transaction.ProccessTransportError();
				Assert.AreEqual(Transaction.States.Terminated, transaction.State);
				Assert.AreEqual(Transaction.Action.InformTuAboutError, action);
			}

			{
				var transaction = GetTransaction(Transaction.States.Proceeding, true);
				int action = transaction.ProccessTransportError();
				Assert.AreEqual(Transaction.States.Proceeding, transaction.State);
				Assert.AreEqual(0, action);
			}
		}

		[Test]
		public void n6_It_should_request_timer_A_for_unreliable_transport_and_timer_B_for_any()
		{
			var reliable = new InviteClientTransaction();
			var e1 = GetServerEventArgs(false);
			int action1 = reliable.ProccessTransactionUser(false, e1);
			Assert.AreEqual(Transaction.Action.StartTimerB, action1 & Transaction.Action.StartTimerB);
			Assert.AreEqual(0, action1 & Transaction.Action.StartTimerA1);
			EventArgsManager.Put(ref e1);

			var unreliable = new InviteClientTransaction();
			var e2 = GetServerEventArgs(true);
			int action2 = unreliable.ProccessTransactionUser(false, e2);
			Assert.AreEqual(Transaction.Action.StartTimerB, action2 & Transaction.Action.StartTimerB);
			Assert.AreEqual(Transaction.Action.StartTimerA1, action2 & Transaction.Action.StartTimerA1);
			EventArgsManager.Put(ref e2);
		}

		[Test]
		public void n7_It_should_request_timer_A1_then_A2_A3_A4_A4_A4_and_etc_in_Calling_state()
		{
			var expectedActions = new int[] {
					Transaction.Action.StartTimerA2,
					Transaction.Action.StartTimerA3,
					Transaction.Action.StartTimerA4,
					Transaction.Action.StartTimerA4,
					Transaction.Action.StartTimerA4,
					Transaction.Action.StartTimerA4,
					Transaction.Action.StartTimerA4,
			};

			var transaction = GetTransaction(Transaction.States.Calling, true);

			foreach (var expectedAction in expectedActions)
				Assert.AreEqual(Transaction.Action.SendCachedMessage | expectedAction, transaction.ProccessTimerA());
		}

		[Test]
		public void n8_It_should_NOT_request_timer_A_for_some_cases()
		{
			var reliable = GetTransaction(Transaction.States.Calling, false);

			for (int i = 0; i < 100; i++)
				Assert.AreEqual(0, reliable.ProccessTimerA());

			foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Calling))
				Assert.AreEqual(0, transaction.ProccessTimerA());
		}

		[Test]
		public void n9_It_should_goto_to_Terminated_by_timer_B_from_Calling_only()
		{
			var reliable = GetTransaction(Transaction.States.Calling, false);
			Assert.AreEqual(0, reliable.ProccessTimerB());
			Assert.AreEqual(Transaction.States.Terminated, reliable.State);

			var unreliable = GetTransaction(Transaction.States.Calling, true);
			Assert.AreEqual(0, unreliable.ProccessTimerB());
			Assert.AreEqual(Transaction.States.Terminated, unreliable.State);

			foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Calling))
			{
				var oldState = transaction.State;
				Assert.AreEqual(0, transaction.ProccessTimerB());
				Assert.AreEqual(oldState, transaction.State);
			}
		}

		[Test]
		public void nA_It_should_request_timer_D_for_unreliable_transport()
		{
			var unreliable = GetTransaction(Transaction.States.Calling, true);
			Assert.AreEqual(Transaction.Action.StartTimerD, unreliable.ProccessTransport(300) & Transaction.Action.StartTimerD);

			var reliable = GetTransaction(Transaction.States.Calling, false);
			Assert.AreEqual(0, reliable.ProccessTransport(300) & Transaction.Action.StartTimerD);
		}

		[Test]
		public void nB_It_should_request_send_ACK_for_300_699_response()
		{
			for (int i = 300; i <= 699; i++)
				foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Terminated))
					Assert.AreEqual(Transaction.Action.SendAck, transaction.ProccessTransport(i) & Transaction.Action.SendAck);

			for (int i = 100; i <= 299; i++)
				foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Terminated))
					Assert.AreEqual(0, transaction.ProccessTransport(i) & Transaction.Action.SendAck);
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
				foreach (var transaction in GetAllTransactionsExcept(Transaction.States.Calling, Transaction.States.Proceeding))
					Assert.AreEqual(0, transaction.ProccessTransport(i) & Transaction.Action.PassIncomingResponse);
		}

		[Test]
		public void nE_It_should_goto_to_Terminated_by_timer_D()
		{
			var unreliable = GetTransaction(Transaction.States.Completed, true);
			unreliable.ProccessTimerD();

			Assert.AreEqual(Transaction.States.Terminated, unreliable.State);
		}

		[Test]
		public void nF_It_should_send_initial_Request()
		{
			{
				var e = GetServerEventArgs(false);
				int action = new InviteClientTransaction().ProccessTransactionUser(false, e);
				Assert.AreEqual(Transaction.Action.SendOutgoingMessage, action & Transaction.Action.SendOutgoingMessage);
				EventArgsManager.Put(e);
			}
			{
				var e = GetServerEventArgs(true);
				int action = new InviteClientTransaction().ProccessTransactionUser(false, e);
				Assert.AreEqual(Transaction.Action.SendOutgoingMessage, action & Transaction.Action.SendOutgoingMessage);
				EventArgsManager.Put(e);
			}
		}

		[Test]
		public void nG_It_should_cache_ack_for_unreliable_transport()
		{
			var e1 = GetServerEventArgs(true);
			e1.Buffer[e1.Offset + 0] = (byte)'A';
			e1.Buffer[e1.Offset + 1] = (byte)'C';
			e1.Buffer[e1.Offset + 2] = (byte)'K';

			var unreliable = GetTransaction(Transaction.States.Completed, true);
			unreliable.ProccessTransactionUser(true, e1);

			using (var e2 = unreliable.GetCachedCopy())
				Assert.AreEqual("ACK", Encoding.UTF8.GetString(e2.Buffer, e2.Offset, 3));

			int action = unreliable.ProccessTransport(300);
			Assert.AreEqual(0, action & Transaction.Action.SendAck);
			Assert.AreEqual(Transaction.Action.SendCachedMessage, action & Transaction.Action.SendCachedMessage);
		}

		protected override IEnumerable<StateUnreliable> GetAllStates()
		{
			return new StateUnreliable[]
			{
				new StateUnreliable(Transaction.States.Calling, false),
				new StateUnreliable(Transaction.States.Calling, true),
				new StateUnreliable(Transaction.States.Proceeding, true),
				new StateUnreliable(Transaction.States.Proceeding, false),
				new StateUnreliable(Transaction.States.Completed, true),
				new StateUnreliable(Transaction.States.Terminated, true),
				new StateUnreliable(Transaction.States.Terminated, false),
			};
		}
	}
}

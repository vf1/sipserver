using System;
using System.Collections.Generic;
using ThreadSafe = System.Collections.Generic.ThreadSafe;
using Sip.Message;
using Sip.Tools;
using SocketServers;
using System.Net;
using System.Text;
using Server.Authorization.Sip;

namespace Sip.Server
{
	sealed class TransactionLayer
	{
		private const int capacity = 16384;

		private const int T1 = 500;
		private const int T2 = 4000;
		private const int T4 = 5000;

		private readonly GeneralVerifier verifier;

		private MultiTimer<int> timerA;
		private MultiTimer<int> timerB;
		private MultiTimer<int> timerD;
		private MultiTimer<int> timerE;
		private MultiTimer<int> timerF;
		private MultiTimer<int> timerG;
		private MultiTimer<int> timerH;
		private MultiTimer<int> timerI;
		private MultiTimer<int> timerJ;
		private MultiTimer<int> timerK;

		private ServerTransactionStorage<NonInviteServerTransaction> nonInviteServerTransactions;
		private ClientTransactionStorage<NonInviteClientTransaction> nonInviteClientTransactions;
		private ServerTransactionStorage<InviteServerTransaction> inviteServerTransactions;
		private ClientTransactionStorage<InviteClientTransaction> inviteClientTransactions;
		private ISipAuthorizationManager authentication;
		private Action<SipMessageWriter> writeSignatureHandler;

		private int[][] acceptedRequestIndexes;
		private AcceptedRequest[] acceptedRequests;
		private ProducedRequest[] producedRequests;

		public Action<ServerAsyncEventArgs> SendAsync;
		public Func<IPAddress, bool> IsLocalAddress;

		public TransactionLayer(ISipAuthorizationManager authentication)
		{
			this.verifier = new GeneralVerifier();

			this.authentication = authentication;
			this.writeSignatureHandler = new Action<SipMessageWriter>(authentication.WriteSignature);

			InitializeTimers();

			inviteServerTransactions = new ServerTransactionStorage<InviteServerTransaction>(capacity, Transaction.Kind.InviteServer, null);
			inviteServerTransactions.GetAcceptedRequestIndex = GetAcceptedRequestIndex;

			nonInviteServerTransactions = new ServerTransactionStorage<NonInviteServerTransaction>(capacity, Transaction.Kind.NonInviteServer, inviteServerTransactions);
			nonInviteServerTransactions.GetAcceptedRequestIndex = GetAcceptedRequestIndex;

			nonInviteClientTransactions = new ClientTransactionStorage<NonInviteClientTransaction>(capacity, Transaction.Kind.NonInviteClient);
			inviteClientTransactions = new ClientTransactionStorage<InviteClientTransaction>(capacity, Transaction.Kind.InviteClient);

			acceptedRequestIndexes = new int[Enum.GetValues(typeof(Methods)).Length][];
			for (int i = 0; i < acceptedRequestIndexes.Length; i++)
				acceptedRequestIndexes[i] = new int[0];
			acceptedRequests = new AcceptedRequest[0];
			producedRequests = new ProducedRequest[0];
		}

		private void InitializeTimers()
		{
			timerA = new MultiTimer<int>(TimerA, capacity);
			timerB = new MultiTimer<int>(TimerB, capacity, 64 * T1);
			timerD = new MultiTimer<int>(TimerD, capacity, 32000);
			timerE = new MultiTimer<int>(TimerE, capacity);
			timerF = new MultiTimer<int>(TimerF, capacity, 64 * T1);
			timerG = new MultiTimer<int>(TimerG, capacity);
			timerH = new MultiTimer<int>(TimerH, capacity, 64 * T1);
			timerI = new MultiTimer<int>(TimerI, capacity, T4);
			timerJ = new MultiTimer<int>(TimerJ, capacity, 64 * T1);
			timerK = new MultiTimer<int>(TimerK, capacity, T4);
		}

		#region Transport Layer Event Handlers

		public void IncomingMessage(IncomingMessage message)
		{
			var result = verifier.Validate(message);
			if (result.Error == GeneralVerifier.Errors.None)
			{
				ProcessIncomingMessage(message);
			}
			else
			{
				var writer = new SipResponseWriter();
				writer.WriteStatusLine(StatusCodes.BadRequest);
				writer.CopyViaToFromCallIdRecordRouteCSeq(message.Reader, StatusCodes.BadRequest);
				writer.WriteXErrorDetails(result.Message, result.HeaderName.ToUtf8Bytes());
				writer.WriteCRLF();

				SendNonTransactionMessage(message.ConnectionAddresses, writer);
			}
		}

		private void ProcessIncomingMessage(IncomingMessage message)
		{
			switch (Transaction.GetBasicKindForIncoming(message.Reader.CSeq.Method, message.Reader.IsRequest))
			{
				case Transaction.Kind.InviteServer:
					{
						InviteServerTransaction transaction;
						int action = inviteServerTransactions.ProccessTransport(message.Reader, out transaction);
						DoWork<InviteServerTransaction>(message, transaction, action);
						break;
					}
				case Transaction.Kind.NonInviteServer:
					{
						NonInviteServerTransaction transaction;
						int action = nonInviteServerTransactions.ProccessTransport(message.Reader, out transaction);
						DoWork<NonInviteServerTransaction>(message, transaction, action);
						break;
					}
				case Transaction.Kind.InviteClient:
					{
						InviteClientTransaction transaction;
						int action = inviteClientTransactions.ProccessTransport(message.Reader, out transaction);
						DoWork<InviteClientTransaction>(message, transaction, action);
						break;
					}
				case Transaction.Kind.NonInviteClient:
					{
						NonInviteClientTransaction transaction;
						int action = nonInviteClientTransactions.ProccessTransport(message.Reader, out transaction);
						DoWork<NonInviteClientTransaction>(message, transaction, action);
						break;
					}

				default:
					throw new NotImplementedException();
			}
		}

		public void TransportError(int transactionKId)
		{
			if (transactionKId != Transaction.InvalidKId)
			{
				switch (Transaction.GetTransactionBasicKind(transactionKId))
				{
					case Transaction.Kind.NonInviteClient:
						{
							NonInviteClientTransaction transaction;
							int action = nonInviteClientTransactions.ProccessTransportError(transactionKId, out transaction);
							DoWork<NonInviteClientTransaction>(transaction, action);
							break;
						}

					case Transaction.Kind.NonInviteServer:
						{
							NonInviteServerTransaction transaction;
							int action = nonInviteServerTransactions.ProccessTransportError(transactionKId, out transaction);
							DoWork<NonInviteServerTransaction>(transaction, action);
							break;
						}

					case Transaction.Kind.InviteClient:
						{
							InviteClientTransaction transaction;
							int action = inviteClientTransactions.ProccessTransportError(transactionKId, out transaction);
							DoWork<InviteClientTransaction>(transaction, action);
							break;
						}

					case Transaction.Kind.InviteServer:
						{
							InviteServerTransaction transaction;
							int action = inviteServerTransactions.ProccessTransportError(transactionKId, out transaction);
							DoWork<InviteServerTransaction>(transaction, action);
							break;
						}

					default:
						throw new InvalidProgramException();
				}
			}
		}

		#endregion

		#region Transaction User Registration

		public void RegisterTransactionUser(BaseTransactionUser transactionUser)
		{
			foreach (var acceptedRequest in transactionUser.GetAcceptedRequests())
			{
				acceptedRequest.ValidateTransactionUserSettings();

				int index = acceptedRequests.Length;

				Array.Resize<AcceptedRequest>(ref acceptedRequests, index + 1);
				acceptedRequests[index] = acceptedRequest;

				int length = acceptedRequestIndexes[(int)acceptedRequest.Method].Length;
				Array.Resize<int>(ref acceptedRequestIndexes[(int)acceptedRequest.Method], length + 1);
				acceptedRequestIndexes[(int)acceptedRequest.Method][length] = index;

				//acceptedRequest.Index = index;
			}

			foreach (var producedRequest in transactionUser.GetProducedRequests())
			{
				int index = producedRequests.Length;

				Array.Resize<ProducedRequest>(ref producedRequests, index + 1);
				producedRequests[index] = producedRequest;

				producedRequest.Index = index;
			}

			transactionUser.SendNonTransactionMessage = SendNonTransactionMessage;
			transactionUser.SendResponseExternal = SendResponse;
			transactionUser.SendRequest = SendRequest;
			transactionUser.GetWriter = GetSipResponseWriter;
			transactionUser.GetTransactionId = GetTransactionId;
			transactionUser.IsLocalAddress = IsLocalAddress;
			transactionUser.Authentication = authentication;
		}

		private int GetAcceptedRequestIndex(SipMessageReader reader)
		{
			int requestHandlerIndex = -1;

			var handlers = acceptedRequestIndexes[(int)reader.Method];

			for (int i = 0; i < handlers.Length; i++)
				if (acceptedRequests[handlers[i]].IsAcceptedRequest(reader))
				{
					requestHandlerIndex = handlers[i];
					break;
				}

			if (requestHandlerIndex < 0)
				//throw new CompatibilityException();
				throw new InvalidProgramException(@"Transaction layer must have at least default handler.");

			return requestHandlerIndex;
		}

		#endregion

		#region Transaction User Event Handlers

		private int GetTransactionId(Methods method)
		{
			if (method == Methods.Invitem)
				return inviteClientTransactions.GetTransactionId();
			else
				return nonInviteClientTransactions.GetTransactionId();
		}

		private SipResponseWriter GetSipResponseWriter()
		{
			var writer = new SipResponseWriter();
			writer.WriteCustomHeadersEvent += writeSignatureHandler;
			return writer;
		}

		private void SendNonTransactionMessage(Transports transport, IPEndPoint localEndPoint, IPEndPoint remoteEndpoint, int connectionId, SipMessageWriter writer)
		{
			SendAsync(
				CreateOutgoingMessage(new ConnectionAddresses(transport, localEndPoint, remoteEndpoint, connectionId),
					writer, Transaction.InvalidKId));
		}

		private void SendNonTransactionMessage(ConnectionAddresses connectionAddresses, SipMessageWriter writer)
		{
			SendAsync(CreateOutgoingMessage(connectionAddresses, writer, Transaction.InvalidKId));
		}

		private void SendRequest(int router, ConnectionAddresses connectionAddresses, int transactionKId, SipMessageWriter writer, int userData)
		{
			bool isAck = writer.Method == Methods.Ackm;
			var e = CreateOutgoingMessage(connectionAddresses, writer, transactionKId);

			int action;
			//switch (Transaction.GetKindForOutgoing(writer.Method, writer.IsRequest))
			switch (Transaction.GetTransactionBasicKind(transactionKId))
			{
				case Transaction.Kind.InviteClient:
					action = inviteClientTransactions.ProccessTransactionUser(transactionKId, router, userData, isAck, e);
					break;

				case Transaction.Kind.NonInviteClient:
					action = nonInviteClientTransactions.ProccessTransactionUser(transactionKId, router, userData, isAck, e);
					break;

				default:
					throw new NotImplementedException();
			}

			DoWork(e, transactionKId, action);
		}

		private void SendResponse(ConnectionAddresses connectionAddresses, int transactionKId, SipMessageWriter writer)
		{
			var e = CreateOutgoingMessage(connectionAddresses, writer, transactionKId);

			int action;
			switch (Transaction.GetTransactionBasicKind(transactionKId))
			{
				case Transaction.Kind.InviteServer:
					action = inviteServerTransactions.ProccessTransactionUser(transactionKId, e, writer.StatusCode);
					break;

				case Transaction.Kind.NonInviteServer:
					action = nonInviteServerTransactions.ProccessTransactionUser(transactionKId, e, writer.StatusCode);
					break;

				default:
					throw new NotImplementedException();
			}

			DoWork(e, transactionKId, action);
		}

		private static ServerAsyncEventArgs CreateOutgoingMessage(ConnectionAddresses ca, SipMessageWriter writer, int transactionKId)
		{
			try
			{
				var e = EventArgsManager.Get();

				e.LocalEndPoint = new ServerEndPoint(ca.Transport.ToServerProtocol(), ca.LocalEndPoint);
				e.RemoteEndPoint = ca.RemoteEndPoint;
				e.ConnectionId = ca.ConnectionId;
				e.UserTokenForSending = transactionKId;

				e.OffsetOffset = 128;
				e.Count = writer.Count;
				e.AllocateBuffer();
				Buffer.BlockCopy(writer.Buffer, writer.Offset, e.Buffer, e.Offset, e.Count);

				return e;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		#endregion

		#region Timers Event Handlers

		private void TimerJ(int transactionKId)
		{
			int action = nonInviteServerTransactions.ProccessTimerJ(transactionKId);
			DoWork(transactionKId, action);
		}

		private void TimerG(int transactionKId)
		{
			int action = inviteServerTransactions.ProccessTimerG(transactionKId);
			DoWork(transactionKId, action);
		}

		private void TimerI(int transactionKId)
		{
			int action = inviteServerTransactions.ProccessTimerI(transactionKId);
			DoWork(transactionKId, action);
		}

		private void TimerH(int transactionKId)
		{
			int action = inviteServerTransactions.ProccessTimerH(transactionKId);
			DoWork(transactionKId, action);
		}

		private void TimerE(int transactionKId)
		{
			int action = nonInviteClientTransactions.ProccessTimerE(transactionKId);
			DoWork(transactionKId, action);
		}

		private void TimerF(int transactionKId)
		{
			NonInviteClientTransaction transaction;
			int action = nonInviteClientTransactions.ProccessTimerF(transactionKId, out transaction);
			DoWork<NonInviteClientTransaction>(transaction, action);
		}

		private void TimerK(int transactionKId)
		{
			int action = nonInviteClientTransactions.ProccessTimerK(transactionKId);
			DoWork(transactionKId, action);
		}

		private void TimerA(int transactionKId)
		{
			int action = inviteClientTransactions.ProccessTimerA(transactionKId);
			DoWork(transactionKId, action);
		}

		private void TimerB(int transactionKId)
		{
			InviteClientTransaction transaction;
			int action = inviteClientTransactions.ProccessTimerB(transactionKId, out transaction);
			DoWork<InviteClientTransaction>(transaction, action);
		}

		private void TimerD(int transactionKId)
		{
			int action = inviteClientTransactions.ProccessTimerD(transactionKId);
			DoWork(transactionKId, action);
		}

		#endregion

		#region DoWork<T>(...) Functions

		private void DoWork(ServerAsyncEventArgs e, int transactionKId, int action)
		{
			if ((action & Transaction.Action.SendOutgoingMessage) > 0)
				SendAsync(e);
			else
				e.Dispose();

			DoWork(transactionKId, action);
		}

		private void DoWork<T>(IncomingMessage message, T transaction, int action)
			where T : ITransaction
		{
			if ((action & Transaction.Action.TransactionNotFound) > 0)
			{
				Tracer.WriteInformation("Transaction not found.");
			}
			else
			{
				if ((action & Transaction.Action.SendAck) > 0)
				{
					var router = producedRequests[transaction.Router];
					router.ProduceAck(new IncomingMessageEx(message, transaction.Id));
				}

				if ((action & Transaction.Action.PassIncomingRequest) > 0)
				{
					var router = acceptedRequests[transaction.Router];
					router.TransactionUser.OnIncomingRequest(router, new IncomingMessageEx(message, transaction.Id));
				}

				if ((action & Transaction.Action.PassIncomingResponse) > 0)
				{
					var router = producedRequests[transaction.Router];
					router.TransactionUser.OnIncomingResponse(router, new IncomingMessageEx(message, transaction.Id));
				}

				if ((action & Transaction.Action.SendCachedMessage) > 0)
				{
					var cached = transaction.GetCachedCopy();
					if (cached != null)
						SendAsync(cached);
				}

				DoWork(transaction.Id, action);
			}
		}

		private void DoWork<T>(T transaction, int action)
			where T : ITransaction
		{
			if ((action & Transaction.Action.InformTuAboutError) > 0)
			{
				if (Transaction.IsClientTransaction(transaction.Id))
					producedRequests[transaction.Router].TransportError(transaction.Id, transaction.UserData);
				else
					acceptedRequests[transaction.Router].TransportError(transaction.Id);
			}

			DoWork(transaction.Id, action);
		}

		private void DoWork(int transactionKId, int action)
		{
			if ((action & Transaction.Action.StartTimerA1) > 0)
				timerA.Add(T1, transactionKId);
			if ((action & Transaction.Action.StartTimerA2) > 0)
				timerA.Add(2 * T1, transactionKId);
			if ((action & Transaction.Action.StartTimerA3) > 0)
				timerA.Add(4 * T1, transactionKId);
			if ((action & Transaction.Action.StartTimerA4) > 0)
				timerA.Add(8 * T1, transactionKId);

			if ((action & Transaction.Action.StartTimerG1) > 0)
				timerG.Add(T1, transactionKId);
			if ((action & Transaction.Action.StartTimerG2) > 0)
				timerG.Add(2 * T1, transactionKId);
			if ((action & Transaction.Action.StartTimerG3) > 0)
				timerG.Add(4 * T1, transactionKId);
			if ((action & Transaction.Action.StartTimerG4) > 0)
				timerG.Add(8 * T1, transactionKId);

			if ((action & Transaction.Action.StartTimerE1) > 0)
				timerE.Add(T1, transactionKId);
			if ((action & Transaction.Action.StartTimerE2) > 0)
				timerE.Add(2 * T1, transactionKId);
			if ((action & Transaction.Action.StartTimerE3) > 0)
				timerE.Add(4 * T1, transactionKId);
			if ((action & Transaction.Action.StartTimerE4) > 0)
				timerE.Add(8 * T1, transactionKId);

			if ((action & Transaction.Action.StartTimerB) > 0)
				timerB.Add(transactionKId);

			if ((action & Transaction.Action.StartTimerD) > 0)
				timerD.Add(transactionKId);

			if ((action & Transaction.Action.StartTimerF) > 0)
				timerF.Add(transactionKId);

			if ((action & Transaction.Action.StartTimerH) > 0)
				timerH.Add(transactionKId);

			if ((action & Transaction.Action.StartTimerI) > 0)
				timerI.Add(transactionKId);

			if ((action & Transaction.Action.StartTimerJ) > 0)
				timerJ.Add(transactionKId);

			if ((action & Transaction.Action.StartTimerK) > 0)
				timerK.Add(transactionKId);
		}

		#endregion
	}
}

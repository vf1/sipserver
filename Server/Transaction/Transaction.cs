using System;
using System.Net;
using System.Collections.Generic;
using Sip.Message;
using SocketServers;

namespace Sip.Server
{
	interface ITransaction
		: IDisposable
	{
		int Id { get; }
		int Router { get; }
		Transaction.States State { get; }
		int UserData { get; }

		int ProccessTransportError();

		ServerAsyncEventArgs GetCachedCopy();
	}

	interface IClientTransaction
		: ITransaction
	{
		void Initialize(int id, int router, int userData);

		bool IsTransportUnreliable { get; }

		int ProccessTransport(int statusCode);
		int ProccessTransactionUser(bool isAck, ServerAsyncEventArgs e);

		int ProccessTimerE();
		int ProccessTimerF();
		int ProccessTimerK();

		int ProccessTimerA();
		int ProccessTimerB();
		int ProccessTimerD();
	}

	interface IServerTransaction
		: ITransaction
	{
		void Initialize(int id, int router, int transactionKeyHashCode);

		int TransactionKeyHashCode { get; }

		int ProccessTransport(bool isAck);
		int ProccessTransactionUser(int statusCode, ServerAsyncEventArgs e);

		int ProccessTimerG();
		int ProccessTimerI();
		int ProccessTimerH();

		int ProccessTimerJ();
	}

	static class Transaction
	{
		public const int InvalidKId = -1;

		public static int GetTransactionKId(Kind kind, int count)
		{
			return (int)kind | (count & 0x00ffffff);
		}

		public static bool IsValidTransactionId(int kid)
		{
			Kind kind = (Kind)(kid & 0xff000000);

			return
				kind == Kind.InviteClient ||
				kind == Kind.InviteServer ||
				kind == Kind.NonInviteClient ||
				kind == Kind.NonInviteServer ||
				kind == Kind.CancelServer ||
				kind == Kind.CancelClient;
		}

		public static int ChangeKind(int sourceKId, Kind kind)
		{
			if (sourceKId == InvalidKId)
				return InvalidKId;

			return sourceKId & 0x00ffffff | (int)kind;
		}

		public static int GetRelaytedInviteServerKId(int cancelKId)
		{
			if ((Kind)(cancelKId & 0xff000000) == Kind.CancelServer)
				return ChangeKind(cancelKId, Kind.InviteServer);
			return InvalidKId;
		}

		public static int GetRelaytedCancelClientKId(int inviteKId)
		{
			if ((Kind)(inviteKId & 0xff000000) != Kind.InviteClient)
				throw new ArgumentOutOfRangeException(@"Kind.InviteClient expected");
			return ChangeKind(inviteKId, Kind.CancelClient);
		}

		public static Kind GetTransactionBasicKind(int kid)
		{
			return (Kind)(kid & 0x0f000000);
		}

		public static Kind GetTransactionKind(int kid)
		{
			return (Kind)(kid & 0xff000000);
		}

		public static bool IsClientTransaction(int kid)
		{
			Kind kind = GetTransactionBasicKind(kid);
			return kind == Kind.InviteClient || kind == Kind.NonInviteClient;
		}

		public static Kind GetBasicKindForIncoming(Methods method, bool isRequest)
		{
			return isRequest ?
				((method == Methods.Invitem || method == Methods.Ackm) ? Kind.InviteServer : Kind.NonInviteServer) :
				((method == Methods.Invitem) ? Kind.InviteClient : Kind.NonInviteClient);
		}

		//public static Kind GetKindForOutgoing(Methods method, bool isRequest)
		//{
		//    return isRequest ?
		//        ((method == Methods.Invitem) ? Kind.InviteClient : Kind.NonInviteClient) :
		//        ((method == Methods.Invitem) ? Kind.InviteServer : Kind.NonInviteServer);
		//}

		public enum Kind
		{
			InviteServer = 0x01000000,
			InviteClient = 0x02000000,
			NonInviteServer = 0x03000000,
			NonInviteClient = 0x04000000,

			CancelServer = 0x10000000 | NonInviteServer,
			CancelClient = 0x20000000 | NonInviteClient,
		};

		public class Action
		{
			public const int PassIncomingRequest = 0x00000001;
			public const int PassIncomingResponse = 0x00000002;
			public const int SendCachedMessage = 0x00000004;
			public const int InformTuAboutError = 0x00000010;
			public const int TransactionNotFound = 0x00000020;
			public const int SendAck = 0x00000040;
			public const int SendOutgoingMessage = 0x00000080;

			public const int StartTimerG1 = 0x00001000;
			public const int StartTimerG2 = 0x00002000;
			public const int StartTimerG3 = 0x00004000;
			public const int StartTimerG4 = 0x00008000;
			public const int StartTimerE1 = 0x00010000;
			public const int StartTimerE2 = 0x00020000;
			public const int StartTimerE3 = 0x00040000;
			public const int StartTimerE4 = 0x00080000;
			public const int StartTimerA1 = 0x00100000;
			public const int StartTimerA2 = 0x00200000;
			public const int StartTimerA3 = 0x00400000;
			public const int StartTimerA4 = 0x00800000;
			public const int StartTimerF = 0x01000000;
			public const int StartTimerJ = 0x02000000;
			public const int StartTimerK = 0x04000000;
			public const int StartTimerH = 0x08000000;
			public const int StartTimerI = 0x10000000;
			public const int StartTimerB = 0x20000000;
			public const int StartTimerD = 0x40000000;
		}

		public enum States
		{
			Created,
			Trying = 1,
			Calling = 1,
			Proceeding,
			Completed,
			Confirmed,
			Terminated,
		};
	}
}

using System;
using System.Net;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;
using SocketServers;

namespace Sip.Server
{
	//enum AcceptCondition
	//{
	//    EmptyToUser,
	//}

	enum AuthorizationMode
	{
		Disabled,
		Enabled,
		Custom,
	}

	sealed class AcceptedRequest
	{
		//public int Index { get; set; }
		public Methods Method { get; set; }
		public Func<SipMessageReader, bool> IsAcceptedRequest { get; set; }
		public Action<AcceptedRequest, IncomingMessageEx> IncomingRequest { get; set; }
		public Action<int> TransportError { get; set; }
		public AuthorizationMode AuthorizationMode { get; set; }
		public BaseTransactionUser TransactionUser { get; private set; }

		public AcceptedRequest(BaseTransactionUser transactionUser)
		{
			//Index = -1;
			TransportError = None;
			//IsAthorizationRequred = true;
			AuthorizationMode = AuthorizationMode.Enabled;
			TransactionUser = transactionUser;
		}

		public void ValidateTransactionUserSettings()
		{
			if (Method == Methods.None)
				throw new ArgumentOutOfRangeException();
			if (IncomingRequest == null)
				throw new ArgumentOutOfRangeException();
			if (IsAcceptedRequest == null)
				throw new ArgumentOutOfRangeException();
			if (TransportError == null)
				throw new ArgumentOutOfRangeException();

			//if (Index != -1)
			//	throw new ArgumentOutOfRangeException();
		}

		public void OnIncomingRequest(IncomingMessageEx message)
		{
			IncomingRequest(this, message);
		}

		/// <summary>
		/// !!! DEPRECATED !!!
		/// </summary>
		public void SendResponse(IncomingMessageEx to, SipMessageWriter writer)
		{
			TransactionUser.SendResponse(to.ConnectionAddresses, to.TransactionId, writer);
		}

		public static bool IsToEqualsFrom(SipMessageReader reader)
		{
			return reader.From.AddrSpec.Value.Equals(reader.To.AddrSpec.Value);
		}

		public static bool IsAccepted(SipMessageReader reader, byte[] type, byte[] subtype, bool isToEqualsFrom)
		{
			return (!isToEqualsFrom || reader.From.AddrSpec.Value.Equals(reader.To.AddrSpec.Value)) &&
				reader.ContentType.Type.Equals(type) &&
				reader.ContentType.Subtype.Equals(subtype);
		}

		public static bool IsAccepted(SipMessageReader reader, ByteArrayPart type, ByteArrayPart subtype, bool isToEqualsFrom)
		{
			return (!isToEqualsFrom || reader.From.AddrSpec.Value.Equals(reader.To.AddrSpec.Value)) &&
				reader.ContentType.Type.Equals(type) &&
				reader.ContentType.Subtype.Equals(subtype);
		}

		public static bool IsAccepted(SipMessageReader reader, ByteArrayPart eventType, ByteArrayPart type, ByteArrayPart subtype, bool isToEqualsFrom)
		{
			return reader.Event.EventType.Equals(eventType) &&
				IsAccepted(reader, type, subtype, isToEqualsFrom);
		}

		public static bool IsAccepted(SipMessageReader reader, ByteArrayPart eventType, bool isToEqualsFrom)
		{
			return reader.Event.EventType.Equals(eventType) &&
				(!isToEqualsFrom || reader.From.AddrSpec.Value.Equals(reader.To.AddrSpec.Value));
		}

		public static bool IsValidContentType(SipMessageReader reader, ByteArrayPart type, ByteArrayPart subtype)
		{
			return reader.ContentType.Type.Equals(type) && reader.ContentType.Subtype.Equals(subtype);
		}

		public static bool IsToUserEmpty(SipMessageReader reader)
		{
			return reader.To.AddrSpec.User.IsInvalid;
		}

		private static void None(int message) { }
	}
}

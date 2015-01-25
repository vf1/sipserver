using System;
using Sip.Message;

namespace Sip.Server
{
	interface IProxie
	{
		int TransactionId { get; }
		ConnectionAddresses ToConnectionAddresses { get; }
		bool IsFinalReceived { get; set; }
		bool IsCancelSent { get; set; }
		int TimerC { get; set; }

		void GenerateForwardedRequest(SipMessageWriter writer, SipMessageReader reader, ArraySegment<byte> content, ConnectionAddresses ca, int serverTransactionId);
		void GenerateForwardedResponse(SipMessageWriter writer, SipMessageReader reader, ArraySegment<byte> content, ConnectionAddresses ca);
		void GenerateCancel(SipMessageWriter writer, SipMessageReader message);
		void GenerateAck(SipMessageWriter writer, SipMessageReader message);

		bool CanFork(SipMessageReader response);
		IProxie Fork(int transactionId);
	}
}

using System;
using Sip.Message;

namespace Sip.Server
{
	class BaseProxie
	{
		public BaseProxie(int transactionId)
		{
			TransactionId = transactionId;
		}

		public int TransactionId { get; private set; }
		public bool IsFinalReceived { get; set; }
		public bool IsCancelSent { get; set; }
		public int TimerC { get; set; }
	}
}

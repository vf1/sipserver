using System;
using System.Collections.Generic;
using Sip.Message;

namespace Sip.Server
{
	sealed class RequestProxy
		: IDisposable
	{
		private readonly BufferHandle bufferHandle;

		private readonly List<IProxie> proxies;

		private bool isAllRequestsSent;
		private int bestResponseStatusCode;
		private SipMessageWriter bestResponse;

		public readonly int ServerTransactionId;
		public readonly ConnectionAddresses ConnectionAddresses;
		public readonly ArraySegment<byte> Headers;
		public readonly ArraySegment<byte> Content;

		public RequestProxy(IncomingMessageEx request)
		{
			this.proxies = new List<IProxie>(1);
			this.bestResponseStatusCode = int.MaxValue;

			this.ServerTransactionId = request.TransactionId;
			this.ConnectionAddresses = request.ConnectionAddresses;

			this.Headers = request.Headers;
			this.Content = request.Content;
			this.bufferHandle = request.DetachBuffers();
		}

		public void Dispose()
		{
			bufferHandle.Free();

			if (bestResponse != null)
			{
				bestResponse.Dispose();
				bestResponse = null;
			}
		}

		public void AddProxie(IProxie proxie)
		{
			proxies.Add(proxie);
		}

		public IProxie GetProxie(int clientTransactionId)
		{
			for (int i = 0; i < proxies.Count; i++)
				if (proxies[i].TransactionId == clientTransactionId)
					return proxies[i];
			return null;
		}

		public List<IProxie> GetAllProxie()
		{
			return proxies;
		}

		public bool IsAllResponsesReceived
		{
			get
			{
				for (int i = 0; i < proxies.Count; i++)
					if (proxies[i].IsFinalReceived == false)
						return false;

				return true;
			}
		}

		public void SetAllRequestsSent()
		{
			isAllRequestsSent = true;
		}

		public bool IsAllRequestsSent
		{
			get
			{
				return isAllRequestsSent;
			}
		}

		public void SetBestResponse(SipMessageWriter response)
		{
			if (bestResponse != response)
			{
				if (bestResponse != null)
				{
					bestResponse.Dispose();
					bestResponse = null;
				}

				bestResponse = response;
				bestResponseStatusCode = response.StatusCode;
			}
		}

		public void SetBestResponseStatusCode(int statusCode)
		{
			bestResponseStatusCode = statusCode;

			if (bestResponse != null)
			{
				bestResponse.Dispose();
				bestResponse = null;
			}
		}

		public SipMessageWriter DetachBestResponse()
		{
			var response = bestResponse;
			bestResponse = null;
			return response;
		}

		public int BestResponseStatusCode
		{
			get { return bestResponseStatusCode; }
		}
	}
}

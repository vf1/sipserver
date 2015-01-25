using System;
using System.Collections.Generic;
using System.Threading;
using Sip.Message;

namespace Sip.Server
{
	partial class ProxyServerTU
	{
		struct RequestProxyFactory
		{
			private readonly ProxyServerTU parent;
			private readonly IncomingMessageEx request;

			private RequestProxy requestProxy;

			public RequestProxyFactory(ProxyServerTU parent, IncomingMessageEx request)
			{
				this.parent = parent;
				this.request = request;

				this.requestProxy = null;
			}

			public void Forward(IProxie proxie)
			{
				if (proxie != null)
				{
					CreateLockedRequestProxy();
					parent.ForwardRequest(proxie, requestProxy, request.Reader);
				}
			}

			public bool HasValue
			{
				get { return requestProxy != null; }
			}

			public RequestProxy Value
			{
				get { return requestProxy; }
			}

			public void SetAllRequestsSent()
			{
				if (requestProxy != null)
					requestProxy.SetAllRequestsSent();
			}

			public void Release()
			{
				if (requestProxy != null)
					Monitor.Exit(requestProxy);
			}

			private void CreateLockedRequestProxy()
			{
				if (requestProxy == null)
				{
					requestProxy = new RequestProxy(request);
					Monitor.Enter(requestProxy);

					if (request.Reader.Method != Methods.Ackm)
						parent.requestProxyes.Add(requestProxy.ServerTransactionId, requestProxy);

					if (request.Reader.Method == Methods.Invitem)
					{
						var writer = parent.GetWriter();
						writer.WriteResponse(request.Reader, StatusCodes.Trying);
						parent.SendResponse(request, writer);
					}
				}
			}
		}
	}
}

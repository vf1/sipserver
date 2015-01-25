using System;
using System.Threading;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;
using Sip.Tools;
using Sip.Server.Accounts;
using ThreadSafe = System.Collections.Generic.ThreadSafe;

namespace Sip.Server
{
	partial class ProxyServerTU
		: BaseTransactionUser
		, IDisposable
	{
		#region struct ServerClientId {...}

		struct ServerClientId
			: IEquatable<ServerClientId>
		{
			public ServerClientId(int serverTransactionId, int clientTransactionId)
			{
				ServerTransactionId = serverTransactionId;
				ClientTransactionId = clientTransactionId;
			}

			public readonly int ServerTransactionId;
			public readonly int ClientTransactionId;

			public bool Equals(ServerClientId that)
			{
				return ServerTransactionId == that.ServerTransactionId && ClientTransactionId == that.ClientTransactionId;
			}
		}

		#endregion

		private ProducedRequest requestProducer;
		private readonly ILocationService locationService;
		private readonly ITrunkManager trunkManager;
		private readonly ThreadSafe.Dictionary<int, int> clientTransactionIds;
		private readonly ThreadSafe.Dictionary<int, RequestProxy> requestProxyes;
		private readonly MultiTimerEx<ServerClientId> timerC;
		private readonly IAccounts accounts;

		[ThreadStatic]
		private static SipMessageReader readerInternal;

		public ProxyServerTU(ILocationService locationService, ITrunkManager traunkManager, IAccounts accounts)
			: this(locationService, traunkManager, 4 * 60 * 1000, accounts)
		{
		}

		public ProxyServerTU(ILocationService locationService, ITrunkManager trunkManager, int delayTimerC, IAccounts accounts)
		{
            this.IsOfficeSIPFiletransferEnabled = true;

			this.locationService = locationService;
			this.trunkManager = trunkManager;
			this.accounts = accounts;

			this.requestProducer = new ProducedRequest(this)
			{
				IncomingResponse = ProccessResponse,
				TransportError = ProccessTransportError,
				ProduceAck = ProduceAck,
			};

			this.clientTransactionIds = new ThreadSafe.Dictionary<int, int>(new Dictionary<int, int>(16384 * 3));
			this.requestProxyes = new ThreadSafe.Dictionary<int, RequestProxy>(new Dictionary<int, RequestProxy>(16384));

			this.timerC = new MultiTimerEx<ServerClientId>(TimerC, 16384, delayTimerC);
		}

		public void Dispose()
		{
			timerC.Dispose();
			clientTransactionIds.Dispose();
			requestProxyes.Dispose();
		}

		#region BaseTransactionUser: .GetAcceptedRequests, .GetProducedRequests

		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			foreach (Methods method in Enum.GetValues(typeof(Methods)))
				if (method != Methods.None && method != Methods.Servicem)
					yield return new AcceptedRequest(this)
					{
						Method = method,
						IsAcceptedRequest = (reader) =>
						{
							if (reader.From.AddrSpec.Value.Equals(reader.To.AddrSpec.Value))
								return false;
							return true;
						},
						IncomingRequest = (method == Methods.Cancelm) ?
							new Action<AcceptedRequest, IncomingMessageEx>(ProccessCancelRequest) : ProccessRequest,
						AuthorizationMode = AuthorizationMode.Custom,
					};
		}

		public override IEnumerable<ProducedRequest> GetProducedRequests()
		{
			yield return requestProducer;
		}

		public override AuthorizationMode OnCustomAuthorization(IncomingMessageEx message)
		{
			return (IsFromLocal(message.Reader) && message.Reader.Method != Methods.Ackm) ?
				AuthorizationMode.Enabled : AuthorizationMode.Disabled;
		}

		#endregion

        public bool IsOfficeSIPFiletransferEnabled
        {
            get;
            set;
        }

		private bool IsFromLocal(SipMessageReader reader)
		{
			//if (reader.From.AddrSpec.Hostport.Host.Equals(domainName))
			//	return true;

			if (accounts.HasDomain(reader.From.AddrSpec.Hostport.Host))
				return true;

			if (IsLocalAddress(reader.From.AddrSpec.Hostport.IP))
				return true;

			return false;
		}

		//private void ProccessRequest(AcceptedRequest tu, IncomingMessageEx request)
		//{
		//    var statusCode = Validate(request.Reader);

		//    if (statusCode == StatusCodes.OK)
		//    {
		//        RequestProxyFactory requestProxy = new RequestProxyFactory(this, tu, request);

		//        try
		//        {
		//            //ByteArrayPart domain, user = request.Reader.RequestUri.User;
		//            //...or from To

		//            var user = request.Reader.To.AddrSpec.User;
		//            var domain = request.Reader.To.AddrSpec.Hostport.Host;

		//            foreach (var binding in locationService.GetEnumerableBindings(user, domain))
		//            {
		//                requestProxy.Forward(binding);
		//            }

		//            if (requestProxy.WasForwarded == false)
		//            {
		//                if (IsFromLocal(request.Reader))
		//                {
		//                    var trunk = trunkManager.GetTrunkByDomain(request.Reader.To.AddrSpec.Hostport.Host);
		//                    if (trunk != null)
		//                        requestProxy.Forward(trunk);
		//                }
		//                else
		//                {
		//                    var trunk = trunkManager.GetTrunkByDomain(request.Reader.From.AddrSpec.Hostport.Host);

		//                    if (trunk != null)
		//                    {
		//                        if (request.Reader.To.Tag.IsValid)
		//                        {
		//                            int tag;
		//                            Dialog dialog1 = null;
		//                            if (HexEncoding.TryParseHex8(request.Reader.To.Tag, out tag))
		//                                dialog1 = trunk.GetDialog1(tag);

		//                            if (dialog1 != null)
		//                                requestProxy.Forward(trunk, tag, dialog1);
		//                        }
		//                        else
		//                        {
		//                            foreach (var binding in locationService.GetEnumerableBindings(trunk.ForwardCallToUri))
		//                            {
		//                                requestProxy.Forward(trunk, binding);
		//                            }
		//                        }
		//                    }
		//                }
		//            }
		//        }
		//        finally
		//        {
		//            if (requestProxy.WasForwarded)
		//            {
		//                requestProxy.RequestProxy.Close();

		//                Monitor.Exit(requestProxy.RequestProxy);
		//                RemoveIfFinished(requestProxy.RequestProxy);
		//            }
		//        }

		//        if (requestProxy.WasForwarded == false)
		//            statusCode = StatusCodes.TemporarilyUnavailable;
		//    }

		//    if (statusCode != StatusCodes.OK)
		//    {
		//        var writer = GetWriter();
		//        writer.WriteResponse(request.Reader, statusCode);
		//        tu.SendResponse(request, writer);
		//    }
		//}

		//private void ProccessRequest(AcceptedRequest tu, IncomingMessageEx request)
		//{
		//    var statusCode = Validate(request.Reader);

		//    if (statusCode == StatusCodes.OK)
		//    {
		//        RequestProxy requestProxy = null;

		//        try
		//        {
		//            //ByteArrayPart domain, user = request.Reader.RequestUri.User;
		//            //...or from To

		//            var user = request.Reader.To.AddrSpec.User;
		//            var domain = request.Reader.To.AddrSpec.Hostport.Host;

		//            foreach (var binding in locationService.GetEnumerableBindings(user, domain))
		//            {
		//                CreateLockedRequestProxy(tu, request, ref requestProxy);
		//                var proxie = new LocalProxie(binding, requestProducer.GetTransactionId(request.Reader.Method));
		//                ForwardRequest(requestProxy, proxie, request.Reader, request.Content);
		//            }

		//            if (requestProxy == null)
		//            {
		//                if (IsFromLocal(request.Reader))
		//                {
		//                    var trunk = trunkManager.GetTrunkByDomain(request.Reader.To.AddrSpec.Hostport.Host);
		//                    if (trunk != null)
		//                    {
		//                        var proxie = new LocalTrunkProxie(requestProducer.GetTransactionId(request.Reader.Method), trunk);
		//                        CreateLockedRequestProxy(tu, request, ref requestProxy);
		//                        ForwardRequest(requestProxy, proxie, request.Reader, request.Content);
		//                    }
		//                }
		//                else
		//                {
		//                    var trunk = trunkManager.GetTrunkByDomain(request.Reader.From.AddrSpec.Hostport.Host);

		//                    if (trunk != null)
		//                    {
		//                        if (request.Reader.To.Tag.IsValid)
		//                        {
		//                            int tag;
		//                            Dialog dialog1 = null;
		//                            if (HexEncoding.TryParseHex8(request.Reader.To.Tag, out tag))
		//                                dialog1 = trunk.GetDialog1(tag);

		//                            if (dialog1 != null)
		//                            {
		//                                var proxie = new TrunkDialogProxie(requestProducer.GetTransactionId(request.Reader.Method), trunk, tag, dialog1);
		//                                CreateLockedRequestProxy(tu, request, ref requestProxy);
		//                                ForwardRequest(requestProxy, proxie, request.Reader, request.Content);
		//                            }
		//                        }
		//                        else
		//                        {
		//                            foreach (var binding in locationService.GetEnumerableBindings(trunk.ForwardCallToUri))
		//                            {
		//                                var proxie = new TrunkLocalProxie(requestProducer.GetTransactionId(request.Reader.Method), trunk, binding);
		//                                CreateLockedRequestProxy(tu, request, ref requestProxy);
		//                                ForwardRequest(requestProxy, proxie, request.Reader, request.Content);
		//                            }
		//                        }
		//                    }
		//                }
		//            }
		//        }
		//        finally
		//        {
		//            if (requestProxy != null)
		//            {
		//                requestProxy.Close();

		//                Monitor.Exit(requestProxy);
		//                RemoveIfFinished(requestProxy);
		//            }
		//        }

		//        if (requestProxy == null)
		//            statusCode = StatusCodes.TemporarilyUnavailable;
		//    }

		//    if (statusCode != StatusCodes.OK)
		//    {
		//        var writer = GetWriter();
		//        writer.WriteResponse(request.Reader, statusCode);
		//        tu.SendResponse(request, writer);
		//    }
		//}

		//private void CreateLockedRequestProxy(AcceptedRequest tu, IncomingMessageEx request, ref RequestProxy requestProxy)
		//{
		//    if (requestProxy == null)
		//    {
		//        requestProxy = new RequestProxy(request);
		//        Monitor.Enter(requestProxy);

		//        ////-----------
		//        if (request.Reader.Method != Methods.Ackm)
		//            requestProxyes.Add(requestProxy.ServerTransactionId, requestProxy);
		//        ////-----------

		//        if (request.Reader.Method == Methods.Invitem)
		//        {
		//            var writer = GetWriter();
		//            writer.WriteResponse(request.Reader, StatusCodes.Trying);
		//            tu.SendResponse(request, writer);
		//        }
		//    }
		//}

		//private void ForwardRequest(RequestProxy requestProxy, IProxie proxie, SipMessageReader reader, ArraySegment<byte> content)
		//{
		//    clientTransactionIds.Add(proxie.TransactionId, requestProxy.ServerTransactionId);

		//    ////-----------
		//    if (reader.Method == Methods.Ackm)
		//        proxie.IsFinalReceived = true;
		//    ////-----------

		//    requestProxy.AddProxie(proxie);

		//    var message = GetWriter();
		//    proxie.GenerateForwardedRequest(Authentication, message, reader, content,
		//        requestProxy.ConnectionAddresses, requestProxy.ServerTransactionId);

		//    requestProducer.SendRequest(proxie.ToConnectionAddresses, message, proxie.TransactionId, requestProxy.ServerTransactionId);

		//    proxie.TimerC = (reader.CSeq.Method == Methods.Invitem) ?
		//        timerC.Add(new ServerClientId(requestProxy.ServerTransactionId, proxie.TransactionId)) : timerC.InvalidTimerIndex;
		//}

		private void ProccessRequest(AcceptedRequest tu, IncomingMessageEx request)
		{
			var statusCode = Validate(request.Reader);

            if (statusCode == StatusCodes.OK)
                statusCode = Filter(request.Reader);

			if (statusCode == StatusCodes.OK)
			{
				RequestProxyFactory requestProxy = new RequestProxyFactory(this, request);

				try
				{
					//ByteArrayPart domain, user = request.Reader.RequestUri.User;
					//...or from To

					var method = request.Reader.Method;
					var user = request.Reader.To.AddrSpec.User;
					var domain = request.Reader.To.AddrSpec.Hostport.Host;

					foreach (var binding in locationService.GetEnumerableBindings(user, domain))
					{
						requestProxy.Forward(
							ProxieFactory.Create(GetTransactionId(method), binding));
					}

					if (requestProxy.HasValue == false)
					{
						if (IsFromLocal(request.Reader))
						{
							var trunk = trunkManager.GetTrunkByDomain(request.Reader.To.AddrSpec.Hostport.Host);
							if (trunk != null)
								requestProxy.Forward(
									ProxieFactory.Create(GetTransactionId(method), trunk));
						}
						else
						{
							var trunk = trunkManager.GetTrunkByDomain(request.Reader.From.AddrSpec.Hostport.Host);

							if (trunk != null)
							{
								if (request.Reader.To.Tag.IsValid)
								{
									requestProxy.Forward(
										ProxieFactory.Create(GetTransactionId(method), trunk, request.Reader.To.Tag));
								}
								else
								{
									foreach (var binding in locationService.GetEnumerableBindings(trunk.ForwardCallToUri))
									{
										requestProxy.Forward(
											ProxieFactory.Create(GetTransactionId(method), trunk, binding));
									}
								}
							}
						}
					}
				}
				finally
				{
					requestProxy.SetAllRequestsSent();
					SendBestIfAllFinalRecived(requestProxy.Value);

					requestProxy.Release();

					RemoveIfFinished(requestProxy.Value);

					if (requestProxy.HasValue == false)
						statusCode = StatusCodes.TemporarilyUnavailable;
				}
			}

			if (statusCode != StatusCodes.OK)
				SendResponse(request, statusCode);
		}

		private void ForwardRequest(IProxie proxie, RequestProxy requestProxy, SipMessageReader reader)
		{
			#region vars...
			var serverConnectionAddresses = requestProxy.ConnectionAddresses;
			var clientConnectionAddresses = proxie.ToConnectionAddresses;
			int serverTransactionId = requestProxy.ServerTransactionId;
			int clientTransactionId = proxie.TransactionId;
			var method = reader.Method;
			var content = requestProxy.Content;
			#endregion

			clientTransactionIds.Add(clientTransactionId, serverTransactionId);

			if (method == Methods.Ackm)
				proxie.IsFinalReceived = true;

			requestProxy.AddProxie(proxie);

			var writer = GetWriter();
			proxie.GenerateForwardedRequest(writer, reader, content, serverConnectionAddresses, serverTransactionId);
			requestProducer.SendRequest(clientConnectionAddresses, writer, clientTransactionId, serverTransactionId);

			proxie.TimerC = (method == Methods.Invitem) ?
				timerC.Add(new ServerClientId(serverTransactionId, clientTransactionId)) : timerC.InvalidTimerIndex;
		}

		private void ProccessCancelRequest(AcceptedRequest tu, IncomingMessageEx request)
		{
			var statusCode = StatusCodes.CallLegTransactionDoesNotExist;

			int inviteTransactionKId = Transaction.GetRelaytedInviteServerKId(request.TransactionId);
			if (inviteTransactionKId != Transaction.InvalidKId)
			{
				var requestProxy = requestProxyes.TryGetValue(inviteTransactionKId);

				if (requestProxy != null)
				{
					CancelClients(requestProxy, request.Reader);
					statusCode = StatusCodes.OK;
				}
			}

			SendResponse(request, statusCode);
		}

		private void ProccessResponse(IncomingMessageEx response)
		{
			var requestProxy = GetRequestProxy(response.TransactionId);

			if (requestProxy != null)
			{
				lock (requestProxy)
				{
					if (requestProxy.IsAllResponsesReceived == false)
					{
						int statusCode = response.Reader.StatusCode.Value;
						int clientTransactionId = response.TransactionId;
						var proxie = requestProxy.GetProxie(clientTransactionId);

						if (proxie != null)
						{
							if (statusCode > 199)
								proxie.IsFinalReceived = true;

							if (statusCode >= 100 && statusCode <= 299)
							{
								Tracer.WriteInformation("Forward response " + statusCode);

								var writer = GetWriter();
								proxie.GenerateForwardedResponse(writer, response.Reader, response.Content, requestProxy.ConnectionAddresses);

								SendResponse(requestProxy.ConnectionAddresses, requestProxy.ServerTransactionId, writer);

								if (statusCode >= 100 && statusCode <= 199)
								{
									if (proxie.TimerC != timerC.InvalidTimerIndex)
										proxie.TimerC = timerC.Change(proxie.TimerC, new ServerClientId(requestProxy.ServerTransactionId, clientTransactionId));
								}

								if (statusCode >= 200 && statusCode <= 299)
								{
									CancelClients(requestProxy, response.Reader);

									if (requestProxy.BestResponseStatusCode > statusCode)
										requestProxy.SetBestResponseStatusCode(statusCode);
								}
							}
							else
							{
								if (proxie.CanFork(response.Reader))
								{
									var proxie2 = proxie.Fork(GetTransactionId(response.Reader.CSeq.Method));
									ForwardRequest(proxie2, requestProxy, ParseHeaders(requestProxy));
								}
								else
								{
									if (requestProxy.BestResponseStatusCode > statusCode)
									{
										var writer = GetWriter();
										proxie.GenerateForwardedResponse(writer, response.Reader, response.Content, requestProxy.ConnectionAddresses);

										requestProxy.SetBestResponse(writer);
									}

									SendBestIfAllFinalRecived(requestProxy);
								}
							}
						}
					}
				}

				RemoveIfFinished(requestProxy);
			}
			else
			{
				// stateless forward
				Tracer.WriteInformation("requestProxy not found for " + response.TransactionId.ToString());
			}
		}

		private void ProduceAck(IncomingMessageEx response)
		{
			var requestProxy = GetRequestProxy(response.TransactionId);

			if (requestProxy != null)
			{
				lock (requestProxy)
				{
					int clientTransactionId = response.TransactionId;
					var proxie = requestProxy.GetProxie(clientTransactionId);

					if (proxie != null)
					{
						var writer = GetWriter();
						proxie.GenerateAck(writer, response.Reader);

						SendAck(proxie.ToConnectionAddresses, writer);
					}
				}
			}
		}

		private void ProccessTransportError(int clientTransactionId, int serverTransactionId)
		{
			//if (Transaction.GetTransactionKind(clientTransactionId) == Transaction.Kind.InviteClient)
			if (Transaction.IsClientTransaction(clientTransactionId))
				ProccessError(serverTransactionId, clientTransactionId, StatusCodes.ServiceUnavailable);
		}

		private void TimerC(ServerClientId id)
		{
			ProccessError(id.ServerTransactionId, id.ClientTransactionId, StatusCodes.RequestTimeout);
		}

		private void ProccessError(int serverTransactionId, int clientTransactionId, StatusCodes statusCode)
		{
			var requestProxy = requestProxyes.TryGetValue(serverTransactionId);
			if (requestProxy != null)
			{
				lock (requestProxy)
				{
					if (requestProxy.IsAllResponsesReceived == false)
					{
						var proxie = requestProxy.GetProxie(clientTransactionId);

						if (proxie != null)
						{
							proxie.IsFinalReceived = true;

							if (requestProxy.BestResponseStatusCode > (int)statusCode)
							{
								requestProxy.SetBestResponse(
									GenerateResponse(ParseHeaders(requestProxy), statusCode));
							}

                            SendBestIfAllFinalRecived(requestProxy);
                        }
					}
				}

				RemoveIfFinished(requestProxy);
			}
		}

		private void CancelClients(RequestProxy requestProxy, SipMessageReader message)
		{
			lock (requestProxy)
			{
				var proxies = requestProxy.GetAllProxie();

				for (int i = 0; i < proxies.Count; i++)
				{
					if (proxies[i].IsFinalReceived == false && proxies[i].IsCancelSent == false)
					{
						proxies[i].IsCancelSent = true;

						if (Transaction.GetTransactionKind(proxies[i].TransactionId) == Transaction.Kind.InviteClient)
						{
							var writer = GetWriter();
							proxies[i].GenerateCancel(writer, message);

							int transactionKId = Transaction.GetRelaytedCancelClientKId(proxies[i].TransactionId);

							requestProducer.SendRequest(proxies[i].ToConnectionAddresses, writer, transactionKId);
						}
					}
				}
			}
		}

		private void SendBestIfAllFinalRecived(RequestProxy requestProxy)
		{
			if (requestProxy != null)
			{
				if (requestProxy.IsAllRequestsSent && requestProxy.IsAllResponsesReceived)
				{
					var bestResponse = requestProxy.DetachBestResponse();
					if (bestResponse != null)
						SendResponse(requestProxy.ConnectionAddresses, requestProxy.ServerTransactionId, bestResponse);
				}
			}
		}

		private RequestProxy GetRequestProxy(int clientTransactionId)
		{
			int serverTransactionId;
			if (clientTransactionIds.TryGetValue(clientTransactionId, out serverTransactionId))
				return requestProxyes.TryGetValue(serverTransactionId);
			return null;
		}

		private void RemoveIfFinished(RequestProxy requestProxy)
		{
			if (requestProxy != null)
			{
				if (requestProxy.IsAllRequestsSent && requestProxy.IsAllResponsesReceived)
				{
					var proxies = requestProxy.GetAllProxie();
					for (int i = 0; i < proxies.Count; i++)
						clientTransactionIds.Remove(proxies[i].TransactionId);

					requestProxy.Dispose();
					requestProxyes.Remove(requestProxy.ServerTransactionId);
				}
			}
		}

		private SipMessageReader ParseHeaders(RequestProxy requestProxy)
		{
			if (readerInternal == null)
				readerInternal = new SipMessageReader();

			readerInternal.SetDefaultValue();
			readerInternal.Parse(requestProxy.Headers.Array, requestProxy.Headers.Offset, requestProxy.Headers.Count);
			readerInternal.SetArray(requestProxy.Headers.Array);

			return readerInternal;
		}

		private static StatusCodes Validate(SipMessageReader reader)
		{
			if (reader.RequestUri.UriScheme != UriSchemes.Sip && reader.RequestUri.UriScheme != UriSchemes.Sips)
				return StatusCodes.UnsupportedURIScheme;

			if (reader.MaxForwards == 0)
				return StatusCodes.TooManyHops;

			if (reader.To.AddrSpec.User.IsInvalid)
				return StatusCodes.BadRequest;

			if (reader.To.AddrSpec.Hostport.Host.IsInvalid)
				return StatusCodes.BadRequest;

			// validate ProxyRequire & Require

			return StatusCodes.OK;
		}

        private static readonly byte[] contentTypeFile = System.Text.Encoding.UTF8.GetBytes("file");
        private static readonly byte[] contentSubtypeData = System.Text.Encoding.UTF8.GetBytes("data");

        private StatusCodes Filter(SipMessageReader reader)
        {
            if (IsOfficeSIPFiletransferEnabled == false)
            {
                if (reader.Method == Methods.Messagem)
                {
                    if (reader.ContentType.Type.Equals(contentTypeFile) && reader.ContentType.Subtype.Equals(contentSubtypeData))
                        return StatusCodes.Forbidden;
                }
            }

            return StatusCodes.OK;
        }

		//    // 1.  Make a copy of the received request
		//    // 2.  Update the Request-URI
		//    // 3.  Update the Max-Forwards header field
		//    // 4.  Optionally add a Record-route header field value
		//    // 5.  Optionally add additional header fields
		//    // 6.  Postprocess routing information
		//    // 7.  Determine the next-hop address, port, and transport
		//    // 8.  Add a Via header field value
		//    // 9.  Add a Content-Length header field if necessary
		//    // 10. Forward the new request
		//    // 11. Set timer C
		//}
	}
}

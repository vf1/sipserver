using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using Sip.Tools;
using Sip.Server;
using Sip.Message;
using Base.Message;
using Sip.Server.Accounts;
using SocketServers;
using Server.Authorization.Sip;

namespace Test
{
	#region class LocationServiceX, class AuthenticationX, class TrunkManagerX

	class LocationServiceX
		: ILocationService
	{
		public Func<string, IEnumerable<LocationService.Binding>> GetEnumerableBindingsEv;

		//public IEnumerable<LocationService.Binding> GetEnumerableBindings(SipMessageReader reader)
		//{
		//    return GetEnumerableBindings(reader.To.AddrSpec.Value);
		//}

		public IEnumerable<LocationService.Binding> GetEnumerableBindings(ByteArrayPart user, ByteArrayPart domain)
		{
			return GetEnumerableBindingsEv("sip:" + user.ToString() + "@" + domain.ToString());
		}

		public IEnumerable<LocationService.Binding> GetEnumerableBindings(ByteArrayPart aor)
		{
			return GetEnumerableBindingsEv(aor.ToString());
		}
	}

	class AuthenticationX
		: ISipAuthorizationManager
	{
		private byte[] response = Encoding.UTF8.GetBytes("AUTHRESPONSE");

		public bool IsAuthorized(SipMessageReader reader, ArraySegment<byte> content, out SipMessageWriter writer)
		{
			writer = null;
			return true;
		}

		public bool IsAuthorized(SipMessageReader reader, ArraySegment<byte> content, ByteArrayPart realm, int param, out SipMessageWriter writer)
		{
			writer = null;
			return true;
		}

		public void WriteSignature(SipMessageWriter writer)
		{
		}

		public byte[] GetDigestResponseHexChars(ByteArrayPart username, ByteArrayPart realm, AuthAlgorithms algorithm,
				ByteArrayPart nonce, int cnonce, int nonceCount, ByteArrayPart password,
				AuthQops qop, ByteArrayPart digestUri, Methods method, ArraySegment<byte> body)
		{
			return response;
		}
	}

	class TrunkManagerX
		: ITrunkManager
	{
		public event Action<Trunk> TrunkAdded;
		public event Action<Trunk> TrunkRemoved;

		public Trunk GetTrunk(ByteArrayPart host)
		{
			return null;
		}

		public Trunk GetTrunkByDomain(ByteArrayPart host)
		{
			return null;
		}

		public Trunk GetTrunkById(int id)
		{
			return null;
		}
	}

	#endregion

	[TestFixture]
	class ProxyServerTUTest
	{
		private ProxyServerTU proxy;
		private int bindingsCount;
		private int[] sendRequestCount;
		private List<SipMessageWriter> sentResponses;
		private List<Request> sendRequestes;
		private int clientTransactionId;
		private int serverTransactionId;
		private int serverCancelTransactionId;
		private List<AcceptedRequest> acceptedRequests;
		private List<ProducedRequest> producedRequests;

		private void CreateProxyServerTU(int bindingsCount1)
		{
			CreateProxyServerTU(bindingsCount1, -1);
		}

		[TearDown]
		public void Cleanup()
		{
			if (sentResponses != null)
			{
				foreach (var response in sentResponses)
					response.Dispose();
				sentResponses = null;
			}

			if (sendRequestes != null)
			{
				foreach (var request in sendRequestes)
					request.Writer.Dispose();
			}

			if (proxy != null)
			{
				proxy.Dispose();
				proxy = null;
			}
		}

		class AccountsShim
			: IAccounts
		{
			public IAccount GetAccount(int id)
			{
				throw new NotImplementedException();
			}

			public IAccount GetAccount(ByteArrayPart domain)
			{
				throw new NotImplementedException();
			}

			public bool HasDomain(ByteArrayPart domain)
			{
				return domain.ToString() == "officesip.local";
			}

			public void ForEach(Action<IAccount> action)
			{
			}

			//IAccount IAccounts.GetDefaultAccount()
			//{
			//    throw new NotImplementedException();
			//}

			//int IAccounts.DefaultAccountId
			//{
			//    get
			//    {
			//        throw new NotImplementedException();
			//    }
			//}
		}

		private void CreateProxyServerTU(int bindingsCount1, int delayTimerC)
		{
			Cleanup();

			sendRequestCount = new int[128];
			sentResponses = new List<SipMessageWriter>();
			sendRequestes = new List<Request>();
			bindingsCount = bindingsCount1;
			clientTransactionId = (int)Transaction.Kind.InviteClient;
			serverTransactionId = int.MaxValue & 0x00ffffff | (int)Transaction.Kind.InviteServer;
			serverCancelTransactionId = int.MaxValue & 0x00ffffff | (int)Transaction.Kind.CancelServer;

			var locationService = new LocationServiceX();
			if (delayTimerC > 0)
				proxy = new ProxyServerTU(locationService, new TrunkManagerX(), delayTimerC, new AccountsShim());
			else
				proxy = new ProxyServerTU(locationService, new TrunkManagerX(), new AccountsShim());


			locationService.GetEnumerableBindingsEv = CreateBindings;

			RegisterTransactionUser(proxy);
		}

		private void RegisterTransactionUser(BaseTransactionUser transactionUser)
		{
			acceptedRequests = new List<AcceptedRequest>();
			producedRequests = new List<ProducedRequest>();

			foreach (var acceptedRequest in transactionUser.GetAcceptedRequests())
			{
				acceptedRequest.ValidateTransactionUserSettings();
				acceptedRequests.Add(acceptedRequest);
			}

			foreach (var producedRequest in transactionUser.GetProducedRequests())
			{
				int index = producedRequests.Count;
				producedRequests.Add(producedRequest);
				producedRequest.Index = index;
			}

			transactionUser.SendNonTransactionMessage = SendNonTransactionMessage;
			transactionUser.SendResponseExternal = SendResponse;
			transactionUser.SendRequest = SendRequest;
			transactionUser.GetWriter = GetSipResponseWriter;
			transactionUser.GetTransactionId = GetTransactionId;
			transactionUser.IsLocalAddress = IsLocalAddress;
			transactionUser.Authentication = new AuthenticationX();
		}

		private IEnumerable<LocationService.Binding> CreateBindings(string aor)
		{
			for (int i = 0; i < bindingsCount; i++)
				yield return CreateBinding(aor + "." + i.ToString());
		}

		private static LocationService.Binding CreateBinding(string addrSpec)
		{
			SipMessageReader reader;
			ArraySegment<byte> headers;

			GetReader("REGISTER sip:x SIP/2.0\r\nContact: <sip:" + addrSpec + ":12345>\r\nCall-ID: call-id\r\n\r\n", out reader, out headers);

			return new LocationService.Binding(reader, 0, 10000, new ConnectionAddresses(Transports.Tcp, GetIPEndPoint(), GetIPEndPoint(), 55555));
		}

		private static IPEndPoint GetIPEndPoint()
		{
			return new IPEndPoint(IPAddress.Loopback, 12345);
		}

		private static void GetReader(string messageText, out SipMessageReader reader, out ArraySegment<byte> headers)
		{
			reader = new SipMessageReader();
			var message = Encoding.UTF8.GetBytes(messageText);

			headers = new ArraySegment<byte>(message);

			reader.SetDefaultValue();
			int parsed = reader.Parse(message, 0, message.Length);
			if (reader.IsFinal == false || reader.IsError || parsed < message.Length)
				throw new InvalidProgramException(@"Invalid message: " + messageText);
			reader.SetArray(message);
		}

		public void PassInviteRequest()
		{
			PassRequest(
				"INVITE sip:2@x SIP/2.0\r\n" +
				"Via: SIP/2.0/TCP 127.0.0.1:45454\r\n" +
				"From: <sip:1@x>\r\n" +
				"To: <sip:2@x>\r\n" +
				"Call-ID: callid\r\n" +
				"CSeq: 1 INVITE\r\n" +
				"\r\n");
		}

		public void PassCancelRequest()
		{
			PassRequest(
				"CANCEL sip:2@x SIP/2.0\r\n" +
				"Via: SIP/2.0/TCP 127.0.0.1:45454\r\n" +
				"From: <sip:1@x>\r\n" +
				"To: <sip:2@x>\r\n" +
				"Call-ID: callid\r\n" +
				"CSeq: 1 CANCEL\r\n" +
				"\r\n");
		}

		public void PassRequest(string message)
		{
			SipMessageReader reader;
			ArraySegment<byte> headers;
			ArraySegment<byte> content = new ArraySegment<byte>();
			GetReader(message, out reader, out headers);

			var im1 = new IncomingMessage(Transports.Tcp, GetIPEndPoint(), GetIPEndPoint(), 44444, reader, headers, content);

			var im2 = new IncomingMessageEx(im1, message.StartsWith("CANCEL") ? serverCancelTransactionId-- : serverTransactionId--);

			bool found = false;
			foreach (var accepted in acceptedRequests)
			{
				if (accepted.Method == im2.Reader.Method)
					if (accepted.IsAcceptedRequest(im2.Reader))
					{
						proxy.OnIncomingRequest(accepted, im2);
						found = true;
					}
			}

			if (found == false)
				throw new InvalidProgramException("Accepted request not found for: " + message);

			//var caBytes = im2.ConnectionAddresses.ToLowerHexChars(im2.TransactionId);

			//return Encoding.UTF8.GetString(caBytes.Array, caBytes.Offset, caBytes.Count);
		}

		public void PassResponse(int router, int statusCode, int transactionId)
		{
			//if (string.IsNullOrEmpty(msRecevied))
			//    throw new ArgumentNullException();

			PassResponse(
				router,
				"SIP/2.0 " + statusCode.ToString() + " OK\r\n" +
				//				"Via: SIP/2.0/TCP 127.0.0.1:5060;branch=z9hG4bK02000001;ms-received-cid=" + msRecevied + "\r\n" +
				"Via: SIP/2.0/TCP 127.0.0.1:5060;branch=z9hG4bK02000001\r\n" +
				"Via: SIP/2.0/TCP 127.0.0.1:45454\r\n" +
				"From: <sip:1@x>\r\n" +
				"To: <sip:2@x>\r\n" +
				"Call-ID: callid\r\n" +
				"CSeq: 1 INVITE\r\n" +
				"\r\n",
				transactionId);
		}

		public void PassResponse(int router, string message, int transactionId)
		{
			SipMessageReader reader;
			ArraySegment<byte> headers;
			ArraySegment<byte> content = new ArraySegment<byte>();
			GetReader(message, out reader, out headers);

			var im1 = new IncomingMessage(Transports.Tcp, new IPEndPoint(IPAddress.None, 22222),
				new IPEndPoint(IPAddress.None, 33333), 111111,
				reader, headers, content);

			var im2 = new IncomingMessageEx(im1, transactionId);

			proxy.OnIncomingResponse(producedRequests[router], im2);
		}

		public void PassClientTransportError(int router, int clientTransactionId, int serverTransactionId)
		{
			producedRequests[router].TransportError(clientTransactionId, serverTransactionId);
		}

		private int GetTransactionId(Methods method)
		{
			return clientTransactionId++;
		}

		private SipResponseWriter GetSipResponseWriter()
		{
			return new SipResponseWriter();
		}

		public bool IsLocalAddress(IPAddress address)
		{
			return true;
		}

		private void SendNonTransactionMessage(Transports transport, IPEndPoint localEndPoint, IPEndPoint remoteEndpoint, int connectionId, SipMessageWriter writer)
		{
		}

		struct Request
		{
			public ConnectionAddresses ConnectionAddresses;
			public int TransactionKId;
			public SipMessageWriter Writer;
		}

		private void SendRequest(int router, ConnectionAddresses connectionAddresses, int transactionKId, SipMessageWriter writer, int userData)
		{
			sendRequestes.Add(new Request() { ConnectionAddresses = connectionAddresses, TransactionKId = transactionKId, Writer = writer, });
			sendRequestCount[(int)writer.Method]++;
		}

		private void SendResponse(ConnectionAddresses connectionAddresses, int transactionKId, SipMessageWriter writer)
		{
			sentResponses.Add(writer);
		}

		[Test]
		public void n1_It_should_send_request_to_each_binding()
		{
			for (int i = 0; i < 32; i++)
			{
				CreateProxyServerTU(i);
				PassInviteRequest();
				Assert.AreEqual(i, sendRequestCount[(int)Methods.Invitem]);
			}
		}

		[Test]
		public void n2_It_should_pass_all_2xx_response_and_cancel_others_after_first_2xx()
		{
			for (int j = 1; j < 32; j++)
			{
				CreateProxyServerTU(j);

				PassInviteRequest();

				for (int i = 1; i <= j; i++)
				{
					PassResponse(0, 200, clientTransactionId + i - j - 1);

					Assert.AreEqual(j - 1, sendRequestCount[(int)Methods.Cancelm]);
				}

				Assert.AreEqual(j, sendRequestCount[(int)Methods.Invitem]);
				Assert.AreEqual(j, sentResponses.Count<SipMessageWriter>(
					(writer) => { return writer.StatusCode >= 200 && writer.Method == Methods.Invitem; }));
			}
		}

		[Test]
		public void n3_It_should_pass_all_1xx_response_before_final_only()
		{
			for (int j = 1; j < 32; j++)
			{
				CreateProxyServerTU(j);

				PassInviteRequest();

				int count1xx = 0;

				for (int i = 1; i <= 255; i++)
				{
					PassResponse(0, 100 + i % 100, clientTransactionId + i % j - j);

					count1xx = sentResponses.Count<SipMessageWriter>((writer) => { return writer.StatusCode >= 100 && writer.StatusCode <= 199; });

					Assert.AreEqual(i + 1, sentResponses.Count);
					Assert.AreEqual(i + 1, count1xx);
				}

				for (int i = 1; i <= j; i++)
					PassResponse(0, 200, (clientTransactionId + i) % j);

				int countBefore1xx = sentResponses.Count;

				for (int i = 1; i <= 255; i++)
					PassResponse(0, 100 + i % 100, (clientTransactionId + i) % j);

				Assert.AreEqual(countBefore1xx, sentResponses.Count);
			}
		}

		[Test]
		public void n4_It_should_send_one_best_response()
		{
			for (int i0 = 300; i0 <= 600; i0 += 100)
				for (int i1 = 300; i1 <= 600; i1 += 100)
					for (int i2 = 300; i2 <= 600; i2 += 100)
						for (int i3 = 200; i3 <= 600; i3 += 100)
							for (int i4 = 300; i4 <= 600; i4 += 100)
							{
								CreateProxyServerTU(5);

								PassInviteRequest();

								PassResponse(0, i0, clientTransactionId - 1);
								PassResponse(0, i1, clientTransactionId - 2);
								PassResponse(0, i2, clientTransactionId - 3);

								Assert.AreEqual(0 + 1, sentResponses.Count);

								PassResponse(0, i3, clientTransactionId - 4);
								PassResponse(0, i4, clientTransactionId - 5);

								int min = Math.Min(i0, Math.Min(Math.Min(i1, i2), Math.Min(i3, i4)));

								Assert.AreEqual(1 + 1, sentResponses.Count);
								Assert.AreEqual(min, sentResponses[1].StatusCode, string.Format("Status Codes: {0} {1} {2} {3} {4}", i0, i1, i2, i3, i4));
							}
		}

		[Test]
		public void n5_It_should_cancel_request_by_cancel()
		{
			for (int j = 1; j < 32; j++)
			{
				CreateProxyServerTU(j);

				PassInviteRequest();

				Assert.AreEqual(0, sendRequestCount[(int)Methods.Cancelm]);

				PassCancelRequest();

				Assert.AreEqual(j, sendRequestCount[(int)Methods.Cancelm]);

				Assert.IsTrue(
					sendRequestes
						.Where<Request>((r) => { return r.Writer.Method == Methods.Cancelm; })
						.All<Request>((r) => { return (r.TransactionKId & 0xff000000) == (int)Transaction.Kind.CancelClient; })
					);
			}
		}

		[Test]
		public void n6_It_should_response_error_on_transport_error()
		{
			for (int j = 1; j < 32; j++)
			{
				CreateProxyServerTU(j);

				PassInviteRequest();

				for (int i = 0; i < j; i++)
				{
					Assert.AreEqual(0 + 1, sentResponses.Count);
					PassClientTransportError(0, clientTransactionId + i - j, serverTransactionId + 1);
				}

				Assert.AreEqual(1 + 1, sentResponses.Count);
				Assert.AreEqual(503, sentResponses[1].StatusCode);
			}
		}

		[Test]
		public void n7_It_should_response_error_on_timer_C()
		{
			CreateProxyServerTU(1, 5000);

			PassInviteRequest();

			PassResponse(0, 100, clientTransactionId - 1);

			Thread.Sleep(4000);

			Assert.AreEqual(1 + 1, sentResponses.Count);
			Assert.AreEqual(100, sentResponses[1].StatusCode);

			Thread.Sleep(2000);

			Assert.AreEqual(2 + 1, sentResponses.Count);
			Assert.AreEqual(408, sentResponses[2].StatusCode);
		}
	}
}

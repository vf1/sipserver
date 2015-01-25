using System;
using System.Threading;
using Sip.Message;

namespace Sip.Server
{
	sealed class LocalProxie
		: BaseProxie
		, IProxie
	{
		private readonly LocationService.Binding binding;
		private int recordRouteIndex;

		public LocalProxie(LocationService.Binding binding, int transactionId)
			: base(transactionId)
		{
			this.binding = binding;
		}

		public bool CanFork(SipMessageReader response)
		{
			return false;
		}

		public IProxie Fork(int transactionId)
		{
			throw new InvalidProgramException();
		}

		public ConnectionAddresses ToConnectionAddresses
		{
			get { return binding.ConnectionAddresses; }
		}

		public void GenerateForwardedRequest(SipMessageWriter writer, SipMessageReader reader, ArraySegment<byte> content, ConnectionAddresses fromConnectionAddress, int serverTransactionId)
		{
			Tracer.WriteInformation("LocalProxie.GenerateForwardedRequest");

			recordRouteIndex = reader.Count.RecordRouteCount;

			writer.WriteRequestLine(reader.Method, binding.AddrSpec);

			//var msReceivedCid = fromConnectionAddress.ToLowerHexChars(serverTransactionId);
			//writer.WriteVia(binding.ConnectionAddresses.Transport, binding.ConnectionAddresses.LocalEndPoint, TransactionId, msReceivedCid);
			//writer.WriteRecordRoute(binding.ConnectionAddresses.Transport, binding.ConnectionAddresses.LocalEndPoint, msReceivedCid);

			writer.WriteVia(binding.ConnectionAddresses.Transport, binding.ConnectionAddresses.LocalEndPoint, TransactionId);
			writer.WriteRecordRoute(binding.ConnectionAddresses.Transport, binding.ConnectionAddresses.LocalEndPoint);

			bool writeContact = true;

			for (int i = 0; i < reader.Count.HeaderCount; i++)
			{
				switch (reader.Headers[i].HeaderName)
				{
					case HeaderNames.MaxForwards:
						{
							int maxForwards = (reader.MaxForwards == int.MinValue) ? 70 : reader.MaxForwards;
							writer.WriteMaxForwards(maxForwards - 1);
						}
						break;

					case HeaderNames.Authorization:
					case HeaderNames.ProxyAuthorization:
						// remove exact authorization header
						break;

					case HeaderNames.To:
						writer.WriteToHeader(reader, i, binding.Epid);
						break;

					case HeaderNames.Contact:
						if (writeContact)
						{
							writer.WriteContact(reader.Contact[0].AddrSpec.User, binding.ConnectionAddresses.LocalEndPoint, binding.ConnectionAddresses.Transport, reader.Contact[0].SipInstance);
							writeContact = false;
						}
						break;

					default:
						writer.WriteHeader(reader, i);
						break;
				}
			}

			if (reader.ContentLength == int.MinValue)
				writer.WriteContentLength(content.Count);

			writer.WriteCustomHeaders();
			writer.WriteCRLF();

			writer.Write(content);
		}

		public void GenerateForwardedResponse(SipMessageWriter writer, SipMessageReader reader, ArraySegment<byte> content, ConnectionAddresses ca)
		{
			writer.WriteStatusLine(reader.StatusCode);

			int recordRouteCount = reader.Count.RecordRouteCount;

			bool writeContact = true;

			for (int i = 1; i < reader.Count.ViaCount; i++)
				writer.WriteHeader(HeaderNames.Via, reader.Via[i].Value);

			for (int i = 0; i < reader.Count.HeaderCount; i++)
			{
				switch (reader.Headers[i].HeaderName)
				{
					case HeaderNames.Via:
						break;

					case HeaderNames.Authorization:
					case HeaderNames.ProxyAuthorization:
						// remove exact authorization header
						break;

					case HeaderNames.Contact:
						if (writeContact)
						{
							writer.WriteContact(reader.Contact[0].AddrSpec.User, ca.LocalEndPoint, ca.Transport, reader.Contact[0].SipInstance);
							writeContact = false;
						}
						break;


					case HeaderNames.RecordRoute:
						{
							recordRouteCount--;
							if (recordRouteCount != recordRouteIndex)
								goto default;

							writer.WriteRecordRoute(ca.Transport, ca.LocalEndPoint);
						}
						break;

					default:
						writer.WriteHeader(reader, i);
						break;
				}
			}

			if (reader.ContentLength == int.MinValue)
				writer.WriteContentLength(content.Count);

			writer.WriteCustomHeaders();
			writer.WriteCRLF();
			writer.Write(content);
		}

		public void GenerateCancel(SipMessageWriter writer, SipMessageReader reader)
		{
			GenerateRequest(writer, reader, Methods.Cancelm);
		}

		public void GenerateAck(SipMessageWriter writer, SipMessageReader reader)
		{
			GenerateRequest(writer, reader, Methods.Ackm);
		}

		private void GenerateRequest(SipMessageWriter writer, SipMessageReader reader, Methods method)
		{
			writer.WriteRequestLine(method, binding.AddrSpec);

			writer.WriteVia(ToConnectionAddresses.Transport, ToConnectionAddresses.LocalEndPoint, TransactionId);

			for (int i = 0; i < reader.Count.HeaderCount; i++)
			{
				switch (reader.Headers[i].HeaderName)
				{
					case HeaderNames.From:
					case HeaderNames.CallId:
						writer.WriteHeader(reader, i);
						break;

					case HeaderNames.To:
						writer.WriteToHeader(reader, i, binding.Epid);
						break;
				}
			}

			writer.WriteMaxForwards(70);
			writer.WriteCseq(reader.CSeq.Value, method);
			writer.WriteContentLength(0);

			writer.WriteCustomHeaders();
			writer.WriteCRLF();
		}
	}
}

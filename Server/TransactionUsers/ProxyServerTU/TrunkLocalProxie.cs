using System;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	class TrunkLocalProxie
		: BaseProxie
		, IProxie
	{
		//private readonly ByteArrayPart toUri;
		//private readonly ByteArrayPart toTag;
		private readonly Trunk trunk;
		private readonly LocationService.Binding binding;

		private int tag;
		private Dialog dialog2;
		private ByteArrayPart via;

		//public TrunkLocalProxie(int transactionId, ByteArrayPart toUri, ByteArrayPart toTag, LocationService.Binding binding, int fromTag, Trunk trunk)
		public TrunkLocalProxie(int transactionId, Trunk trunk, LocationService.Binding binding)
			: base(transactionId)
		{
			//this.toUri = toUri;
			//this.toTag = toTag;
			//this.tag = fromTag;
			this.trunk = trunk;
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
			//dialog2 = new Dialog(reader, fromTag, fromConnectionAddress);

			Dialog dialog1 = null;
			if (HexEncoding.TryParseHex8(reader.To.Tag, out tag))
				dialog1 = trunk.GetDialog1(tag);
			else
				tag = DialogManager.NewLocalTag();

			dialog2 = new Dialog(reader, tag, fromConnectionAddress);
			trunk.AddDialog2(tag, dialog2);

			/////////////////////////////////////////////////////////////////////////////////

			writer.WriteRequestLine(reader.Method, binding.AddrSpec);

			//var msReceivedCid = fromConnectionAddress.ToLowerHexChars(serverTransactionId);
			//writer.WriteVia(binding.ConnectionAddresses.Transport, binding.ConnectionAddresses.LocalEndPoint, TransactionId, msReceivedCid);
			writer.WriteVia(binding.ConnectionAddresses.Transport, binding.ConnectionAddresses.LocalEndPoint, TransactionId);

			for (int i = 0; i < reader.Count.HeaderCount; i++)
			{
				switch (reader.Headers[i].HeaderName)
				{
					case HeaderNames.MaxForwards:
						writer.WriteMaxForwards((reader.MaxForwards == int.MinValue) ? 69 : reader.MaxForwards - 1);
						break;

					case HeaderNames.Contact:
						writer.WriteContact(binding.ConnectionAddresses.LocalEndPoint, binding.ConnectionAddresses.Transport);
						break;

					case HeaderNames.To:
						writer.WriteTo(trunk.ForwardCallToUri, (dialog1 != null) ? dialog1.RemoteTag : ByteArrayPart.Invalid);
						break;

					case HeaderNames.From:
						writer.WriteFrom(reader.From.AddrSpec.Value, tag);
						break;

					case HeaderNames.Authorization:
						break;

					default:
						writer.WriteHeader(reader, i);
						break;

					case HeaderNames.Via:
						via = reader.Headers[i].Value.DeepCopy();
						break;
				}
			}

			if (reader.ContentLength == int.MinValue)
				writer.WriteContentLength(content.Count);

			writer.WriteCustomHeaders();
			writer.WriteCRLF();

			writer.Write(content);
		}

		public void GenerateForwardedResponse(SipMessageWriter writer, SipMessageReader response, ArraySegment<byte> content, ConnectionAddresses ca)
		{
			//if (response.StatusCode.Is2xx && response.To.Tag.IsValid)
			//    trunk.AddDialogs(tag, new Dialog(response, ca), dialog2);


			if (response.CSeq.Method == Methods.Byem)
			{
				trunk.RemoveDialog1(tag);
				trunk.RemoveDialog2(tag);
			}
			else
			{
				if (response.StatusCode.Is2xx && response.To.Tag.IsValid)
					trunk.AddDialog1(tag, new Dialog(response, binding.ConnectionAddresses)); //ca));
				if (response.StatusCode.Is2xx == false && response.StatusCode.Is1xx == false)
					trunk.RemoveDialog2(tag);
			}


			writer.WriteStatusLine(response.StatusCode);

			for (int i = 0; i < response.Count.HeaderCount; i++)
			{
				switch (response.Headers[i].HeaderName)
				{
					case HeaderNames.Via:
						writer.Write(SipMessageWriter.C.Via, SipMessageWriter.C.HCOLON, via);
						writer.WriteCRLF();
						break;

					case HeaderNames.WwwAuthenticate:
					case HeaderNames.ProxyAuthenticate:
						break;

					case HeaderNames.To:
						writer.WriteTo(dialog2.LocalUri, dialog2.LocalTag);
						break;

					case HeaderNames.From:
						writer.WriteFrom(dialog2.RemoteUri, dialog2.RemoteTag);
						break;

					case HeaderNames.CSeq:
						writer.WriteCseq(dialog2.RemoteCseq, response.CSeq.Method);
						break;

					case HeaderNames.Contact:
						writer.WriteContact(ca.LocalEndPoint, ca.Transport);
						break;

					default:
						writer.WriteHeader(response, i);
						break;
				}
			}

			if (response.ContentLength == int.MinValue)
				writer.WriteContentLength(content.Count);

			writer.WriteCustomHeaders();
			writer.WriteCRLF();
			writer.Write(content);
		}

		public void GenerateCancel(SipMessageWriter writer, SipMessageReader reader)
		{
			trunk.RemoveDialog1(tag);
			trunk.RemoveDialog2(tag);

			GenerateRequest(writer, reader, Methods.Cancelm);
		}

		public void GenerateAck(SipMessageWriter writer, SipMessageReader reader)
		{
			GenerateRequest(writer, reader, Methods.Ackm);
		}

		private void GenerateRequest(SipMessageWriter writer, SipMessageReader reader, Methods method)
		{
			writer.WriteRequestLine(method, trunk.ForwardCallToUri);

			writer.WriteVia(ToConnectionAddresses.Transport, ToConnectionAddresses.LocalEndPoint, TransactionId);

			for (int i = 0; i < reader.Count.HeaderCount; i++)
			{
				switch (reader.Headers[i].HeaderName)
				{
					case HeaderNames.From:
					case HeaderNames.To:
					case HeaderNames.CallId:
						writer.WriteHeader(reader, i);
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

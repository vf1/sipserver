using System;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	class TrunkDialogProxie
		: BaseProxie
		, IProxie
	{
		private readonly Trunk trunk;
		private readonly Dialog dialog1;
		private readonly int tag;

		private Dialog dialog2;
		private ByteArrayPart via;

		public TrunkDialogProxie(int transactionId, Trunk trunk, int tag, Dialog dialog1)
			: base(transactionId)
		{
			this.trunk = trunk;
			this.tag = tag;
			this.dialog1 = dialog1;
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
			get { return dialog1.ConnectionAddresses; }
		}

		public void GenerateForwardedRequest(SipMessageWriter writer, SipMessageReader reader, ArraySegment<byte> content, ConnectionAddresses fromConnectionAddress, int serverTransactionId)
		{
			dialog2 = new Dialog(reader, tag, fromConnectionAddress);
			//trunk.AddDialog2(tag, dialog2);

			/////////////////////////////////////////////////////////////////////////////////

			writer.WriteRequestLine(reader.Method, dialog1.RemoteUri);

			//var msReceivedCid = fromConnectionAddress.ToLowerHexChars(serverTransactionId);
			//writer.WriteVia(dialog1.Transport, dialog1.LocalEndPoint, TransactionId, msReceivedCid);
			writer.WriteVia(dialog1.Transport, dialog1.LocalEndPoint, TransactionId);

			for (int i = 0; i < reader.Count.HeaderCount; i++)
			{
				switch (reader.Headers[i].HeaderName)
				{
					case HeaderNames.MaxForwards:
						writer.WriteMaxForwards((reader.MaxForwards == int.MinValue) ? 69 : reader.MaxForwards - 1);
						break;

					case HeaderNames.Contact:
						writer.WriteContact(dialog1.LocalEndPoint, dialog1.Transport);
						break;

					case HeaderNames.To:
						writer.WriteTo(dialog1.RemoteUri, dialog1.RemoteTag);
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

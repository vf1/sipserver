using System;
using System.Threading;
using Sip.Message;
using Base.Message;
using Server.Authorization.Sip;

namespace Sip.Server
{
	class LocalTrunkProxie
		: BaseProxie
		, IProxie
	{
		private readonly static ByteArrayPart sdp = new ByteArrayPart("sdp");

		private readonly Trunk trunk;
		private ByteArrayPart via;
		private int tag;
		private Dialog dialog1;
		//private Dialog dialog2;

		public LocalTrunkProxie(int transactionId, Trunk trunk)
			: base(transactionId)
		{
			this.trunk = trunk;
		}

		public bool CanFork(SipMessageReader response)
		{
			if (response.StatusCode.Value != 407 && response.StatusCode.Value != 401)
				return false;

			return trunk.UpdateChallenge(response.GetAnyChallenge());
		}

		public IProxie Fork(int transactionId)
		{
			return new LocalTrunkProxie(transactionId, trunk)
			{
				tag = tag,
				//dialog2 = dialog2,
				dialog1 = dialog1,
				via = via,
			};
		}

		public ConnectionAddresses ToConnectionAddresses
		{
			get { return trunk.ConnectionAddresses; }
		}

		public int Tag
		{
			get { return tag; }
		}

		public void GenerateForwardedRequest(SipMessageWriter writer, SipMessageReader reader, ArraySegment<byte> content, ConnectionAddresses fromConnectionAddress, int serverTransactionId)
		{
			//int? tag = HexEncoding.ParseHex8(reader.To.Tag);
			//var dialog2 = trunk.GetExternalDialog(tag);

			//if (dialog2 != null)
			//{
			//    this.tag = tag.Value;
			//    //this.dialog1 = dialog1;
			//    this.dialog2 = dialog2;
			//}

			Dialog dialog2 = null;
			if (HexEncoding.TryParseHex8(reader.To.Tag, out tag))
				dialog2 = trunk.GetDialog2(tag);
			else
				tag = DialogManager.NewLocalTag();

			dialog1 = new Dialog(reader, tag, fromConnectionAddress);
			trunk.AddDialog1(tag, dialog1);


			writer.WriteRequestLine(reader.Method, trunk.Transport.ToScheme(), reader.To.AddrSpec.User, trunk.Domain);

			//var msReceivedCid = fromConnectionAddress.ToLowerHexChars(serverTransactionId);
			//writer.WriteVia(trunk.ConnectionAddresses.Transport, trunk.ConnectionAddresses.LocalEndPoint, TransactionId, msReceivedCid);
			writer.WriteVia(trunk.ConnectionAddresses.Transport, trunk.ConnectionAddresses.LocalEndPoint, TransactionId);

			for (int i = 0; i < reader.Count.HeaderCount; i++)
			{
				switch (reader.Headers[i].HeaderName)
				{
					case HeaderNames.MaxForwards:
						writer.WriteMaxForwards((reader.MaxForwards == int.MinValue) ? 69 : reader.MaxForwards - 1);
						break;

					case HeaderNames.Contact:
						writer.WriteContact(trunk.LocalEndPoint, trunk.Transport, reader.Contact[0].SipInstance);
						break;

					case HeaderNames.Via:
						via = reader.Headers[i].Value.DeepCopy();
						break;

					case HeaderNames.To:
						writer.WriteTo2(reader.To.AddrSpec.User, trunk.Domain, (dialog2 != null) ? dialog2.RemoteTag : ByteArrayPart.Invalid);
						break;

					case HeaderNames.From:
						writer.WriteFrom(trunk.Uri, tag);
						break;

					case HeaderNames.CSeq:
						writer.WriteCseq(trunk.GetCSeq(reader.CSeq.Method, reader.CallId, reader.CSeq.Value), reader.CSeq.Method);
						break;

					case HeaderNames.Authorization:
					case HeaderNames.ContentLength:
					case HeaderNames.Extension:
						break;

					default:
						writer.WriteHeader(reader, i);
						break;
				}
			}

			WriteAuthorization(writer, reader.Method, content);

			if (reader.Method == Methods.Invitem && reader.ContentType.Subtype.Equals(sdp)) // temp
				content = Sip.Sdp.Helpers.CutCandidates(content);

			writer.WriteContentLength(content.Count);
			writer.WriteCRLF();

			writer.Write(content);
		}

		/// Что с этой функцией делать?! Как отсюда убрать? Убрать в Helpers?
		private void WriteAuthorization(SipMessageWriter writer, Methods method, ArraySegment<byte> content)
		{
			if (trunk.Nonce.IsValid)
			{
				int nc = trunk.GetNextNonceCount();
				int cnonce = Environment.TickCount;

				var response = SipDigestAuthentication.GetResponseHexChars(trunk.AuthenticationId, trunk.Realm, AuthAlgorithms.Md5, trunk.Nonce,
					cnonce, nc, trunk.Password, trunk.Qop, trunk.Uri, method.ToByteArrayPart(), content);

				writer.WriteDigestAuthorization(trunk.AuthHeader, trunk.AuthenticationId, trunk.Realm, trunk.Qop, AuthAlgorithms.Md5, trunk.Uri,
					trunk.Nonce, nc, cnonce, trunk.Opaque, response);
			}
		}

		public void GenerateForwardedResponse(SipMessageWriter writer, SipMessageReader response, ArraySegment<byte> content, ConnectionAddresses ca)
		{
			//if (dialog2 == null && response.StatusCode.Is2xx && response.To.Tag.IsValid)
			//    trunk.AddDialogs(tag, dialog1, new Dialog(response, ca));

			if (response.CSeq.Method == Methods.Byem)
			{
				trunk.RemoveDialog1(tag);
				trunk.RemoveDialog2(tag);
			}
			else
			{
				if (response.StatusCode.Is2xx && response.To.Tag.IsValid)
					trunk.AddDialog2(tag, new Dialog(response, ca));
				if (response.StatusCode.Is2xx == false && response.StatusCode.Is1xx == false)
					trunk.RemoveDialog1(tag);
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
						writer.WriteTo(dialog1.LocalUri, dialog1.LocalTag);
						break;

					case HeaderNames.From:
						writer.WriteFrom(dialog1.RemoteUri, dialog1.RemoteTag);
						break;

					case HeaderNames.CSeq:
						writer.WriteCseq(dialog1.RemoteCseq, response.CSeq.Method);
						break;

					case HeaderNames.Contact:
						writer.WriteContact(response.To.AddrSpec.User, ca.LocalEndPoint, ca.Transport, ByteArrayPart.Invalid);
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
			//trunk.RemoveDialogs(tag);

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
			writer.WriteRequestLine(method, trunk.Uri);

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

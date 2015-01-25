using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;
using Sip.Server.Accounts;
using SocketServers;
using Mras;

namespace Sip.Server
{
	sealed class MrasTU
		: BaseTransactionUser
	{
		private readonly ByteArrayPart type;
		private readonly ByteArrayPart subtype;
		private readonly byte[] userPrefix;
		private readonly Mras1 mras;

		public MrasTU(Mras1 mras)
		{
			this.type = new ByteArrayPart("application");
			this.subtype = new ByteArrayPart("msrtc-media-relay-auth+xml");
			this.userPrefix = Encoding.UTF8.GetBytes("MRASLoc.");

			this.mras = mras;
		}

		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			yield return new AcceptedRequest(this)
			{
				Method = Methods.Servicem,
				IsAcceptedRequest = (reader) => IsAccepted(reader),
				IncomingRequest = ProccessService,
			};
		}

		public bool IsAccepted(SipMessageReader reader)
		{
			return
				reader.ContentType.Type.Equals(type) &&
				reader.ContentType.Subtype.Equals(subtype) &&
				reader.To.AddrSpec.User.StartsWith(userPrefix);
		}

		private void ProccessService(AcceptedRequest tu, IncomingMessageEx request)
		{
			var writer = GetWriter();

			try
			{
				var outContent = mras.ProcessRequest(request.Content);

				writer.WriteStatusLine(StatusCodes.OK);
				writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, StatusCodes.OK);
				writer.WriteContentType(type, subtype);
				writer.WriteCustomHeaders();
				writer.WriteContentLength();
				writer.WriteCRLF();

				writer.Write(outContent.GenerateToByteArray());
				writer.RewriteContentLength();
			}
			catch (MrasException)
			{
				writer.WriteResponse(request.Reader, StatusCodes.BadRequest);
			}

			tu.SendResponse(request, writer);
		}
	}
}

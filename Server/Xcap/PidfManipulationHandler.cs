using System;
using Sip.Simple;
using Base.Message;
using Http.Message;

namespace Server.Xcap
{
	class PidfManipulationHandler
		: BasePidfManipulationHandler
	{
		private readonly SimpleModule simpleModule;

		public PidfManipulationHandler(SimpleModule simpleModule)
		{
			this.simpleModule = simpleModule;
		}

		public override HttpMessageWriter ProcessGetItem(ByteArrayPart username, ByteArrayPart domain)
		{
			var document = simpleModule.GetDocument(username.ToString() + "@" + domain.ToString());

			if (document == null)
			{
				return base.CreateResponse(StatusCodes.NotFound);
			}
			else
			{
				var response = base.CreateNotFinishedResponse(StatusCodes.OK, ContentType.ApplicationPidfXml);

				document.WriteLenghtAndContent(response);

				return response;
			}
		}

		public override HttpMessageWriter ProcessPutItem(ByteArrayPart username, ByteArrayPart domain, HttpMessageReader reader, ArraySegment<byte> content)
		{
			var statusCode = StatusCodes.OK;

			int sipIfMatch = simpleModule.InvalidEtag;
			if (reader.Count.IfMatches > 0)
			{
				if (HexEncoding.TryParseHex8(reader.IfMatches[0].Bytes, reader.IfMatches[0].Begin, out sipIfMatch) == false)
					statusCode = StatusCodes.PreconditionFailed;
			}

			if (statusCode == StatusCodes.OK)
			{
				if (simpleModule.Publish(username.ToString() + "@" + domain.ToString(), ref sipIfMatch, 60, content) == false)
					statusCode = StatusCodes.BadRequest;
			}

			HttpMessageWriter response;
			if (statusCode != StatusCodes.OK)
			{
				response = CreateResponse(statusCode);
			}
			else
			{
				response = CreateNotFinishedResponse(statusCode, ContentType.None);

				response.WriteEtag(sipIfMatch);
				response.WriteCRLF();
			}

			return response;
		}
	}
}

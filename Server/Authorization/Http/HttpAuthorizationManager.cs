using System;
using Base.Message;
using Http.Message;

namespace Server.Authorization.Http
{
	class HttpAuthorizationManager
		: AuthorizationManager<HttpMessageReader, HttpMessageWriter, AuthSchemes>
		, IHttpAuthorizationManager
	{
		public Func<HttpMessageReader, ByteArrayPart, int, bool> ValidateAuthorization { set; get; }

		protected override bool ValidateAuthorizationInternal(HttpMessageReader reader, ByteArrayPart username, int param)
		{
			return ValidateAuthorization(reader, username, param);
		}

		protected override HttpMessageWriter GetResponseBegin(HttpMessageReader reader)
		{
			var writer = new HttpMessageWriter();
			writer.WriteStatusLine(StatusCodes.Unauthorized);
			writer.WriteAccessControlAllowOrigin(true);
			writer.WriteAccessControlAllowCredentials(true);
			writer.WriteAccessControlAllowMethods(Methods.Get, Methods.Put, Methods.Post, Methods.Delete, Methods.Options);
			writer.WriteAccessControlAllowHeaders(System.Text.Encoding.UTF8.GetBytes("Authorization"));
			writer.WriteAccessControlExposeHeaders(System.Text.Encoding.UTF8.GetBytes("WWW-Authenticate"));

			return writer;
		}

		protected override void WriteMessageEnd(HttpMessageWriter writer)
		{
			writer.WriteContentLength(0);
			writer.WriteCRLF();
		}

		protected override bool CanTryAgainWhenFail(AuthSchemes scheme)
		{
			return scheme == AuthSchemes.Digest;
		}
	}
}

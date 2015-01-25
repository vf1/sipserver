using System;
using System.Text;
using Http.Message;

namespace Sip.Server
{
	static class AjaxWebsocketWriter
	{
		private static byte[] accessControlAllowHeaders = Encoding.UTF8.GetBytes("Authorization");
		private static byte[] accessControlExposeHeaders = Encoding.UTF8.GetBytes("WWW-Authenticate");

		public static void WriteNotFinishedResponse(this HttpMessageWriter writer, StatusCodes statusCode, ContentType contentType)
		{
			writer.WriteStatusLine(statusCode);
			writer.WriteAccessControlHeaders();
			if (contentType != ContentType.None)
				writer.WriteContentType(contentType);
			else
				writer.WriteContentLength(0);
		}

		public static void WriteResponse(this HttpMessageWriter writer, StatusCodes statusCodes, ContentType contentType, byte[] content)
		{
			writer.WriteStatusLine(statusCodes);
			writer.WriteAccessControlHeaders();
			writer.WriteContentType(contentType);
			writer.WriteContentLength(content.Length);
			writer.WriteCRLF();
			writer.Write(content);
		}

		public static void WriteEmptyResponse(this HttpMessageWriter writer, StatusCodes statusCode)
		{
			writer.WriteStatusLine(statusCode);
			writer.WriteAccessControlHeaders();
			writer.WriteContentLength(0);
			writer.WriteCRLF();
		}

		public static void WriteAccessControlHeaders(this HttpMessageWriter writer)
		{
			writer.WriteAccessControlAllowOrigin(true);
			writer.WriteAccessControlAllowCredentials(true);
			writer.WriteAccessControlAllowHeaders(accessControlAllowHeaders);
			writer.WriteAccessControlExposeHeaders(accessControlExposeHeaders);
			writer.WriteAccessControlAllowMethods(Methods.Get, Methods.Post, Methods.Options);
		}
	}
}

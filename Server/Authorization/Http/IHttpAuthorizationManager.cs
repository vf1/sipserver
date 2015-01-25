using System;
using Base.Message;
using Http.Message;

namespace Server.Authorization.Http
{
	interface IHttpAuthorizationManager
		: IAuthorizationManager<HttpMessageReader, HttpMessageWriter>
	{
		Func<HttpMessageReader, ByteArrayPart, int, bool> ValidateAuthorization { set; }
	}
}

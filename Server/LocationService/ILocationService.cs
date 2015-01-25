using System;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	interface ILocationService
	{
		//IEnumerable<LocationService.Binding> GetEnumerableBindings(SipMessageReader reader);
		IEnumerable<LocationService.Binding> GetEnumerableBindings(ByteArrayPart user, ByteArrayPart domain);
		IEnumerable<LocationService.Binding> GetEnumerableBindings(ByteArrayPart aor);
	}
}

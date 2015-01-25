using System;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	static class ProxieFactory
	{
		public static IProxie Create(int transactionId, LocationService.Binding binding)
		{
			return new LocalProxie(binding, transactionId);
		}

		public static IProxie Create(int transactionId, Trunk trunk)
		{
			return new LocalTrunkProxie(transactionId, trunk);
		}

		public static IProxie Create(int transactionId, Trunk trunk, ByteArrayPart toTag)
		{
			int tag;
			Dialog dialog1 = null;
			if (HexEncoding.TryParseHex8(toTag, out tag))
				dialog1 = trunk.GetDialog1(tag);

			return (dialog1 == null) ? null : new TrunkDialogProxie(transactionId, trunk, tag, dialog1);
		}

		public static IProxie Create(int transactionId, Trunk trunk, LocationService.Binding binding)
		{
			return new TrunkLocalProxie(transactionId, trunk, binding);
		}
	}
}

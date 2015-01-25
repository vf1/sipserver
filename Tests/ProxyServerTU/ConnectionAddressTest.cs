using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using Sip.Tools;
using Sip.Server;
using Sip.Message;
using Base.Message;
using SocketServers;

namespace Test
{
	//[TestFixture]
	//class ConnectionAddressTest
	//{
	//    [Test]
	//    public void it_should_parse_this_connection_address()
	//    {
	//        int transactionId;
	//        var addresses = ConnectionAddresses.Parse(new ByteArrayPart("020000000313c404d014f60aef2b041e14f60a01000002"), out transactionId);

	//        Assert.AreEqual(0x01000002, transactionId);
	//        Assert.IsTrue(addresses.HasValue);
	//        Assert.AreEqual("10.246.20.30:61227", addresses.Value.RemoteEndPoint.ToString());
	//        Assert.AreEqual("10.246.20.208:5060", addresses.Value.LocalEndPoint.ToString());
	//        Assert.AreEqual(Transports.Tcp, addresses.Value.Transport);
	//    }
	//}
}

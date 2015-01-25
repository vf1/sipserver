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
	[TestFixture]
	class LocalTrunkProxieTest
	{
		private string request;
		private string response;
		private string content =
			"v=0\r\n" +
			"o=- 0 0 IN IP4 127.0.0.1\r\n" +
			"s=session\r\n" +
			"c=IN IP4 127.0.0.1\r\n" +
			"t=0 0\r\n" +
			"m=message 5060 sip null\r\n" +
			"a=accept-types:application/ms-imdn+xml \r\n";


		private Trunk trunk;
		private IProxie proxie;
		private ConnectionAddresses destAddr;
		private ConnectionAddresses srcAddr;

		public LocalTrunkProxieTest()
		{
			trunk = new Trunk("Display Name", "trunk.domain", "user", Transports.Udp,
				new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234), "server.url", "username", "password", "forward-to@officesip.local", 0)
			{
				//Nonce = new ByteArrayPart("NONCEVALUE"),
				//Opaque = new ByteArrayPart("OPAQUEVALUE"),
				AuthHeader = HeaderNames.Authorization,
			};
			trunk.UpdateChallenge(new ByteArrayPart("NONCEVALUE"), new ByteArrayPart("OPAQUEVALUE"), new ByteArrayPart("auth"));

			proxie = new LocalTrunkProxie(0x12345678, trunk);

			destAddr = new ConnectionAddresses(Transports.Udp, new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234), new IPEndPoint(IPAddress.Parse("9.0.0.0"), 9000), 99);
			srcAddr = new ConnectionAddresses(Transports.Tcp, new IPEndPoint(IPAddress.Parse("5.6.7.8"), 5678), new IPEndPoint(IPAddress.Parse("8.0.0.0"), 8000), 88);

			request = CreateForwardedRequest(
				"INVITE sip:jdoe7@officesip.local SIP/2.0\r\n" +
				"Via: SIP/2.0/TCP 127.0.0.1:11371\r\n" +
				"Max-Forwards: 70\r\n" +
				"From: <sip:jdoe2@officesip.local>;tag=123;epid=456\r\n" +
				"To: <sip:jdoe7@officesip.local>\r\n" +
				"Call-ID: 403d20dd4ff84d178cd187b5710cb9a8\r\n" +
				"CSeq: 25 INVITE\r\n" +
				"Contact: <sip:jdoe2@officesip.local:11371;maddr=127.0.0.1;transport=tcp>;proxy=replace;+sip.instance=\"<urn:uuid:6984F470-4B9A-5F59-90C1-C9CA88CF214A>\"\r\n" +
				"Authorization: Digest username=\"jdoe2\", realm=\"officesip.local\", qop=auth, algorithm=MD5, uri=\"sip:jdoe7@officesip.local\", nonce=\"c01ba269fbd8eb84d29a52bd8ba7ba7e\", nc=6, cnonce=\"85550150622448769693330628644602\", opaque=\"00000001\", response=\"feed390d2fcaa5e65f0ce57eaa64756c\"\r\n" +
				"Content-Type: application/sdp\r\n" +
				"Content-Length: 135\r\n" +
				"\r\n",
				content);

			response = CreateForwardedResponse(
				"SIP/2.0 200 OK\r\n" +
				"Via: SIP/2.0/UDP 1.2.3.4:1234;branch=z9hG4bK12345678\r\n" +
				"Record-Route: <sip:1.2.3.4:1234;lr>\r\n" +
				"Via: SIP/2.0/TCP 127.0.0.1:11371\r\n" +
				"Max-Forwards: 69\r\n" +
				"From: \"Display Name\" <sip:user@trunk.domain>;tag=123;epid=456\r\n" +
				"To: <sip:jdoe7@trunk.domain>;tag=321\r\n" +
				"Call-ID: callid\r\n" +
				"CSeq: 1 INVITE\r\n" +
				"Contact: <sip:55.55.55.55:5555;transport=udp>;+sip.instance=\"<urn:uuid:123>\"\r\n" +
				"Content-Type: application/sdp\r\n" +
				"WWW-Authenticate: Digest realm=\"xxx\",nonce=\"0d0f70e6aa82aac734235c10f150271f\",qop=\"auth\",algorithm=MD5,stale=false,opaque=\"00000001\"\r\n" +
				"Content-Length: 135\r\n",
				content);
		}

		private string GetTag()
		{
			return (proxie as LocalTrunkProxie).Tag.ToString("00000000");
		}

		[Test]
		public void Q_It_should_create_correct_request_line()
		{
			Assert.IsTrue(request.StartsWith("INVITE sip:jdoe7@trunk.domain SIP/2.0\r\n"));
		}

		[Test]
		public void Q_It_should_add_Via()
		{
			ContainText(request, "\r\nVia: SIP/2.0/UDP 1.2.3.4:1234;branch=z9hG4bK12345678");
		}

		[Test]
		public void Q_It_should_replace_CSeq()
		{
			ContainText(request, "\r\nCSeq: 1 INVITE\r\n");
		}

		[Test]
		public void Q_It_should_remove_original_Authorization()
		{
			Assert.IsFalse(request.Contains("Authorization: Digest username=\"jdoe2\","));
		}

		[Test]
		public void Q_It_should_add_new_Authorization()
		{
			Assert.IsTrue(request.Contains("Authorization: Digest username=\"username\","));
		}

		[Test]
		public void Q_It_should_replace_uri_in_From_and_add_dispay_name()
		{

			ContainText(request, "From: <sip:user@trunk.domain>;tag=" + GetTag() + "\r\n");
		}

		[Test]
		public void Q_It_should_replace_uri_in_To()
		{
			ContainText(request, "To: <sip:jdoe7@trunk.domain>\r\n");
		}

		[Test]
		public void Q_It_should_replace_Contact()
		{
			ContainText(request, "Contact: <sip:1.2.3.4:1234;transport=udp>;+sip.instance=\"<urn:uuid:6984F470-4B9A-5F59-90C1-C9CA88CF214A>\"\r\n");
		}

		[Test]
		public void Q_It_should_copy_content()
		{
			ContainText(request, content);
		}

		[Test]
		public void RES_It_should_remove_up_Via()
		{
			Assert.IsFalse(response.Contains("Via: SIP/2.0/UDP 1.2.3.4:1234"));
		}

		[Test]
		public void RES_It_should_copy_content()
		{
			ContainText(response, content);
		}

		[Test]
		public void RES_It_should_restore_CSeq()
		{
			ContainText(response, "CSeq: 25 INVITE\r\n");
		}

		[Test]
		public void RES_It_should_restore_uri_in_From()
		{
			ContainText(response, "From: <sip:jdoe2@officesip.local>;tag=123\r\n"); //;epid=456\r\n");
		}

		[Test]
		public void RES_It_should_restore_uri_in_To()
		{
			ContainText(response, "To: <sip:jdoe7@officesip.local>;tag=" + GetTag() + "\r\n");
		}

		[Test]
		public void RES_It_should_remove_WWW_Authenticate()
		{
			Assert.IsFalse(response.Contains("WWW-Authenticate"));
		}

		[Test]
		public void RES_It_should_change_Contact()
		{
			//ContainText(response, "Contact: <sip:5.6.7.8:5678;transport=tcp>\r\n");
			ContainText(response, "Contact: <sip:jdoe7@5.6.7.8:5678;transport=tcp>\r\n");
		}

		private string CreateForwardedRequest(string message, string content)
		{
			var reader = Parse(message);
			var contentBytes = Encoding.UTF8.GetBytes(content);

			using (var writer = new SipMessageWriter())
			{
				proxie.GenerateForwardedRequest(writer, reader, new ArraySegment<byte>(contentBytes), destAddr, 0x1000);
				return Encoding.UTF8.GetString(writer.Buffer, writer.Offset, writer.Count);
			}
		}

		private string CreateForwardedResponse(string message, string content)
		{
			var reader = Parse(message);

			using (var writer = new SipMessageWriter())
			{
				proxie.GenerateForwardedResponse(writer, reader, new ArraySegment<byte>(Encoding.UTF8.GetBytes(content)), srcAddr);
				return Encoding.UTF8.GetString(writer.Buffer, writer.Offset, writer.Count);
			}
		}

		private static SipMessageReader Parse(string message)
		{
			var messageBytes = Encoding.UTF8.GetBytes(message);

			var reader = new SipMessageReader();
			reader.SetDefaultValue();
			reader.Parse(messageBytes, 0, messageBytes.Length);
			reader.SetArray(messageBytes);
			return reader;
		}

		public void ContainText(string all, string text)
		{
			if (all.Contains(text) == false)
			{
				int similar = all.IndexOf(text.Substring(0, 4));

				if (similar >= 0)
					Assert.AreEqual(text, all.Substring(similar, text.Length));
				else
					Assert.Fail("Not found.");
			}
		}
	}
}

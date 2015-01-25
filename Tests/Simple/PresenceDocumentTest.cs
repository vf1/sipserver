using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using NUnit.Framework;
using Sip.Simple;
using Sip.Message;

namespace Simple
{
	[TestFixture]
	class PresenceDocumentTest
	{
		private static string openPresence = @"<?xml version='1.0' encoding='UTF-8'?><presence xmlns='urn:ietf:params:xml:ns:pidf'>";
		private static string closePresence = @"</presence>";

		[Test]
		public void It_should_generate_valid_empty_document()
		{
			AreEqual("<tuple id='sipserver0123'><status><basic>close</basic></status><contact>sip:test@domain.com</contact><note>Offline</note></tuple>", 
				new PresenceDocument("test@domain.com"));
		}

		[Test]
		public void It_should_add_tuples()
		{
			var doc = new PresenceDocument("test@domain.com");
			doc.Modify(0, GetPresenceBytes("<tuple id='1'></tuple>"));

			AreEqual("<tuple id='1' xmlns='urn:ietf:params:xml:ns:pidf'></tuple>", doc);

			doc.Modify(1, GetPresenceBytes("<tuple id='2'></tuple>"));

			AreEqual("<tuple id='2' xmlns='urn:ietf:params:xml:ns:pidf'></tuple><tuple id='1' xmlns='urn:ietf:params:xml:ns:pidf'></tuple>", doc);
		}

		[Test]
		public void It_should_remove_tuples_if_it_absent_in_new_request()
		{
			var doc = new PresenceDocument("test@domain.com");
			doc.Modify(0, GetPresenceBytes("<tuple id='1'></tuple>"));
			doc.Modify(0, GetPresenceBytes("<tuple id='2'></tuple>"));

			AreEqual("<tuple id='2' xmlns='urn:ietf:params:xml:ns:pidf'></tuple>", doc);
		}

		[Test]
		public void It_should_replace_tuples_with_same_id()
		{
			var doc = new PresenceDocument("test@domain.com");
			doc.Modify(0, GetPresenceBytes("<tuple id='1'></tuple>"));
			doc.Modify(1, GetPresenceBytes("<tuple id='1'><new/></tuple>"));

			AreEqual("<tuple id='1' xmlns='urn:ietf:params:xml:ns:pidf'><new /></tuple>", doc);
		}

		[Test]
		public void It_should_remove_tuples_by_sipIfMatch()
		{
			var doc = new PresenceDocument("test@domain.com");
			doc.Modify(0, GetPresenceBytes("<tuple id='1'></tuple><tuple id='2'></tuple>"));
			doc.Modify(1, GetPresenceBytes("<tuple id='2'></tuple>"));
			doc.Remove(0);

			AreEqual("<tuple id='2' xmlns='urn:ietf:params:xml:ns:pidf'></tuple>", doc);
		}

		private void AreEqual(string expected, PresenceDocument doc)
		{
			var writer = new SipMessageWriter();

			expected = expected.Replace('\'', '"');

			//byte[] actualBytes = null;
			//int copied = doc.CopyTo(
			//    (length) => { return new ArraySegment<byte>(actualBytes = new byte[length], 0, length); });
			doc.WriteContent(writer);

			//string actual = Encoding.UTF8.GetString(actualBytes);

			var actual = Encoding.UTF8.GetString(writer.Buffer, writer.Offset, writer.Count);

			Assert.AreEqual(openPresence + expected + closePresence, actual);
		}

		private static ArraySegment<byte> GetPresenceBytes(string text)
		{
			return new ArraySegment<byte>(Encoding.UTF8.GetBytes(openPresence + text + closePresence));
		}
	}
}

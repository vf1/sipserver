using System.IO;
using System.Xml;
using ServiceSoap.XmlContent;
using NUnit.Framework;

namespace ServiceSoap
{
	[TestFixture]
	public class DirectorySearchRequestTest
	{
		[Test]
		public void ParseTest()
		{
			string xml1 = "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\"><SOAP-ENV:Body><m:directorySearch xmlns:m=\"http://schemas.microsoft.com/winrtc/2002/11/sip\"><m:filter m:href=\"#searchArray\"/><m:maxResults>100</m:maxResults></m:directorySearch><m:Array xmlns:m=\"http://schemas.microsoft.com/winrtc/2002/11/sip\" m:id=\"searchArray\"><m:row m:attrib=\"givenName\" m:value=\"n\"/><m:row m:attrib=\"givenEmail\" m:value=\"e\"/><m:row m:attrib=\"ltgtamp\" m:value=\"&lt;&gt;&amp;\"/></m:Array></SOAP-ENV:Body></SOAP-ENV:Envelope>";
			string xml2 = "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\"><SOAP-ENV:Body><m:directorySearch xmlns:m=\"http://schemas.microsoft.com/winrtc/2002/11/sip\"><m:filter m:href=\"#searchArray\"/><m:maxResults>100</m:maxResults></m:directorySearch><m:Array xmlns:m=\"http://schemas.microsoft.com/winrtc/2002/11/sip\" m:id=\"searchArray\"/></SOAP-ENV:Body></SOAP-ENV:Envelope>";

			var request1 = DirectorySearchRequest.Parse(CreateXmlReader(xml1));

			Assert.AreEqual(100, request1.MaxResults);
			Assert.AreEqual(3, request1.SearchTerms.Count);
			Assert.AreEqual("n", request1.SearchTerms["givenName"]);
			Assert.AreEqual("e", request1.SearchTerms["givenEmail"]);
			Assert.AreEqual("<>&", request1.SearchTerms["ltgtamp"]);
			
			var request2 = DirectorySearchRequest.Parse(CreateXmlReader(xml2));

			Assert.AreEqual(100, request2.MaxResults);
			Assert.AreEqual(0, request2.SearchTerms.Count);
		}

		public XmlReader CreateXmlReader(string xml)
		{
			using (MemoryStream stream = new MemoryStream())
			using (StreamWriter writer = new StreamWriter(stream))
			{
				writer.Write(xml);
				writer.Flush();
				stream.Seek(0, SeekOrigin.Begin);

				return XmlReader.Create(stream);
			}
		}
	}
}

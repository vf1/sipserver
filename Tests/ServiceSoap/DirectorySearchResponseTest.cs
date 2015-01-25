using System.Xml;
using System.Text;
using System.Collections.Generic;
using ServiceSoap.XmlContent;
using NUnit.Framework;

namespace ServiceSoap
{
	[TestFixture]
	public class DirectorySearchResponseTest
	{
		[Test]
		public void GenerateTest()
		{
			string xml1 = "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:m=\"http://schemas.microsoft.com/winrtc/2002/11/sip\"><SOAP-ENV:Body><m:directorySearch><m:moreAvailable>false</m:moreAvailable><m:returned>0</m:returned><m:value m:href=\"#rows\" /></m:directorySearch><m:Array m:id=\"rows\" /></SOAP-ENV:Body></SOAP-ENV:Envelope>";
			string xml2 = "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:m=\"http://schemas.microsoft.com/winrtc/2002/11/sip\"><SOAP-ENV:Body><m:directorySearch><m:moreAvailable>false</m:moreAvailable><m:returned>2</m:returned><m:value m:href=\"#rows\" /></m:directorySearch><m:Array m:id=\"rows\"><m:row m:uri=\"uri1\" m:displayName=\"name2\" m:title=\"title3\" m:office=\"office4\" m:phone=\"phone5\" m:company=\"company6\" m:city=\"city7\" m:state=\"state8\" m:country=\"country9\" m:email=\"email10\" /><m:row m:uri=\"uri21\" m:displayName=\"name22\" m:title=\"title23\" m:office=\"office24\" m:phone=\"phone25\" m:company=\"company26\" m:city=\"city27\" m:state=\"state28\" m:country=\"country29\" m:email=\"email210\" /></m:Array></SOAP-ENV:Body></SOAP-ENV:Envelope>";
			string xml3 = "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:m=\"http://schemas.microsoft.com/winrtc/2002/11/sip\"><SOAP-ENV:Body><m:directorySearch><m:moreAvailable>false</m:moreAvailable><m:returned>1</m:returned><m:value m:href=\"#rows\" /></m:directorySearch><m:Array m:id=\"rows\"><m:row m:uri=\"\" m:displayName=\"\" m:title=\"\" m:office=\"\" m:phone=\"\" m:company=\"\" m:city=\"\" m:state=\"\" m:country=\"\" m:email=\"\" /></m:Array></SOAP-ENV:Body></SOAP-ENV:Envelope>";

			DirectorySearchResponse response0 = new DirectorySearchResponse();

			DirectorySearchResponse response1 = new DirectorySearchResponse()
			{
				Items = new List<DirectorySearchItem>(),
			};

			Assert.AreEqual(xml1.Length, GenerateXml(response0).GetEqualLength(xml1));
			Assert.AreEqual(xml1.Length, GenerateXml(response1).GetEqualLength(xml1));

			DirectorySearchResponse response2 = new DirectorySearchResponse()
			{
				Items = new List<DirectorySearchItem>()
				{
					new DirectorySearchItem()
					{
						Uri = @"uri1",
						DisplayName = @"name2",
						Title = @"title3",
						Office = @"office4",
						Phone = @"phone5",
						Company = @"company6",
						City  = @"city7",
						State = @"state8",
						Country = @"country9",
						Email = @"email10",
					},
					new DirectorySearchItem()
					{
						Uri = @"uri21",
						DisplayName = @"name22",
						Title = @"title23",
						Office = @"office24",
						Phone = @"phone25",
						Company = @"company26",
						City  = @"city27",
						State = @"state28",
						Country = @"country29",
						Email = @"email210",
					},
				},
			};

			Assert.AreEqual(xml2.Length, GenerateXml(response2).GetEqualLength(xml2));

			DirectorySearchResponse response3 = new DirectorySearchResponse()
			{
				Items = new List<DirectorySearchItem>()
				{
					new DirectorySearchItem(),
				},
			};

			Assert.AreEqual(xml3.Length, GenerateXml(response3).GetEqualLength(xml3));
		}

		private string GenerateXml(EnhancedPresence.OutContentBase content)
		{
			return (new EnhancedPresence.OutContent(content, new object())).GenerateToString();
		}
	}
}

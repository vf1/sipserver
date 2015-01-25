using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml;

namespace ServiceSoap.XmlContent
{
	class DirectorySearchRequest
	{
		public DirectorySearchRequest()
		{
			SearchTerms = new Dictionary<string, string>();
		}

		public int MaxResults { get; set; }
		public Dictionary<string, string> SearchTerms { get; set; }

		public static DirectorySearchRequest Parse(XmlReader reader)
		{
			DirectorySearchRequest request = new DirectorySearchRequest();

			reader.MoveToContent();

			if (reader.LocalName != @"Envelope" || reader.LookupNamespace(reader.Prefix) != @"http://schemas.xmlsoap.org/soap/envelope/")
				throw new XmlException();

			reader.Read();
			reader.MoveToElement();

			if (reader.LocalName != @"Body")
				throw new XmlException();

			reader.Read();
			reader.MoveToElement();

			const string schema2 = @"http://schemas.microsoft.com/winrtc/2002/11/sip";
			if (reader.LocalName != @"directorySearch" || reader.LookupNamespace(reader.Prefix) != schema2)
				throw new XmlException();

			reader.Read();
			reader.MoveToElement();
			if (reader.LocalName != @"filter")
				throw new XmlException();
			string filter = reader.GetAttribute(@"href", schema2);

			reader.Read();
			reader.MoveToElement();
			request.MaxResults = reader.ReadElementContentAsInt(@"maxResults", schema2);

			reader.Read();
			reader.MoveToElement();
			if (reader.LocalName != @"Array") 
				throw new XmlException();
			string id = reader.GetAttribute(@"id", schema2);
			if (filter[0] != '#' || filter.Substring(1) != id)
				throw new XmlException();

			reader.Read();
			reader.MoveToElement();
			string z = reader.GetAttribute(@"value");
			while (reader.LocalName == @"row")
			{
				string attrib = reader.GetAttribute(@"attrib", schema2);
				string value = reader.GetAttribute(@"value", schema2);

				if (string.IsNullOrEmpty(attrib) == false)
				{
					if (request.SearchTerms.ContainsKey(attrib))
						request.SearchTerms[attrib] = value;
					else
						request.SearchTerms.Add(attrib, value);
				}

				reader.Read();
				reader.MoveToElement();
			}

			reader.MoveToContent();
			reader.ReadEndElement();

			return request;
		}
	}
}

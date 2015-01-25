using System;
using System.Collections.Generic;
using System.Xml;

namespace ServiceSoap.XmlContent
{
	public class DirectorySearchItem
	{
		public string Uri { get; set; }
		public string DisplayName { get; set; }
		public string Title { get; set; }
		public string Office { get; set; }
		public string Phone { get; set; }
		public string Company { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string Country { get; set; }
		public string Email { get; set; }
	}

	public class DirectorySearchResponse
		: EnhancedPresence.OutContentBase
	{
		public bool MoreAvailable { get; set; }
		public List<DirectorySearchItem> Items { get; set; }

		public override void Generate(XmlWriter writer)
		{
			const string soapSchema = @"http://schemas.xmlsoap.org/soap/envelope/";
			const string winrtcSchema = "http://schemas.microsoft.com/winrtc/2002/11/sip";

			writer.WriteStartElement(@"SOAP-ENV", @"Envelope", soapSchema);
			writer.WriteAttributeString("xmlns", "SOAP-ENV", null, soapSchema);
			writer.WriteAttributeString("xmlns", "m", null, winrtcSchema);
			writer.WriteStartElement(@"Body", soapSchema);

			writer.WriteStartElement(@"m", @"directorySearch", winrtcSchema);

			writer.WriteStartElement(@"moreAvailable", winrtcSchema);
			writer.WriteValue(MoreAvailable);
			writer.WriteEndElement();
			writer.WriteElementString(@"returned", winrtcSchema, (Items != null) ? Items.Count.ToString() : @"0");
			writer.WriteStartElement(@"value", winrtcSchema);
			writer.WriteAttributeString(@"href", winrtcSchema, @"#rows");
			writer.WriteEndElement();

			writer.WriteEndElement();   // @"directorySearch"

			writer.WriteStartElement(@"Array", winrtcSchema);
			writer.WriteAttributeString(@"id", winrtcSchema, @"rows");

			if (Items != null)
				foreach (var item in Items)
				{
					writer.WriteStartElement(@"row", winrtcSchema);
					writer.WriteAttributeString(@"uri", winrtcSchema, item.Uri);
					writer.WriteAttributeString(@"displayName", winrtcSchema, item.DisplayName);
					writer.WriteAttributeString(@"title", winrtcSchema, item.Title);
					writer.WriteAttributeString(@"office", winrtcSchema, item.Office);
					writer.WriteAttributeString(@"phone", winrtcSchema, item.Phone);
					writer.WriteAttributeString(@"company", winrtcSchema, item.Company);
					writer.WriteAttributeString(@"city", winrtcSchema, item.City);
					writer.WriteAttributeString(@"state", winrtcSchema, item.State);
					writer.WriteAttributeString(@"country", winrtcSchema, item.Country);
					writer.WriteAttributeString(@"email", winrtcSchema, item.Email);
					writer.WriteEndElement();
				}

			writer.WriteEndElement();

			writer.WriteEndElement();
			writer.WriteEndElement();
		}

		public override string OutContentType
		{
			get { return "application"; }
		}

		public override string OutContentSubtype
		{
			get { return @"SOAP+xml"; }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml;

namespace Mras.XmlContent
{
	public class Response
		: EnhancedPresence.OutContentBase
	{
		public Request Request { get; set; }
		public Version ServerVersion { get; set; }
		public ReasonPhrase ReasonPhrase { get; set; }
		public IEnumerable<CredentialsResponse> CredentialsResponses { get; set; }

		public override void Generate(XmlWriter writer)
		{
			writer.WriteRaw("<?xml version=\"1.0\"?>");
			writer.WriteStartElement(@"response", @"http://schemas.microsoft.com/2006/09/sip/mrasp");

			if (Request != null)
			{
				writer.WriteAttributeString(@"requestID", Request.RequestId);
				writer.WriteAttributeString(@"version", Request.Version);
			}
			
			writer.WriteAttributeString(@"serverVersion", ServerVersion);
			
			if (Request != null)
			{
				writer.WriteAttributeString(@"to", Request.To);
				writer.WriteAttributeString(@"from", Request.From);
			}
			
			writer.WriteAttributeString(@"reasonPhrase", Common.ToString(ReasonPhrase));

			if (CredentialsResponses != null)
				foreach (var credentialsResponse in CredentialsResponses)
					credentialsResponse.Generate(writer);

			writer.WriteEndElement();
		}

		public override string OutContentType
		{
			get { return @"application"; }
		}

		public override string OutContentSubtype
		{
			get { return @"msrtc-media-relay-auth+xml"; }
		}
	}
}

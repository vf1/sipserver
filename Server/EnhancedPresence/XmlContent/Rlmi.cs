using System;
using System.Text;
using System.Xml;
using System.IO;

namespace EnhancedPresence
{
	// Resource List Meta-Information
	public class Rlmi
		: OutContentBase
		, IDisposable
	{
		private XmlWriter writer;
		private StringBuilder output;

		protected Rlmi(string uri)
		{
			output = new StringBuilder();

			XmlWriterSettings settings = new XmlWriterSettings();
			settings.CloseOutput = true;
			settings.OmitXmlDeclaration = true;

			writer = XmlWriter.Create(output, settings);

			writer.WriteStartElement(@"list", @"urn:ietf:params:xml:ns:rlmi");
			writer.WriteAttributeString(@"uri", uri);
			writer.WriteAttributeString(@"version", @"0");
			writer.WriteAttributeString(@"fullState", @"false");
		}

		public static Rlmi Create(string uri)
		{
			return new Rlmi(uri);
		}

		public void Dispose()
		{
			writer.WriteEndElement();

			writer.Flush();
			writer.Close();
		}

		public override void Generate(XmlWriter writer1)
		{
			this.writer.Flush();
			this.writer.Close();

			writer1.WriteRaw(output.ToString());
		}

		public override string OutContentType
		{
			get { return @"application"; }
		}

		public override string OutContentSubtype
		{
			get { return @"rlmi+xml"; }
		}

		private static string GetCid(string uri)
		{
			if (uri.StartsWith(@"sip:"))
				return uri.Substring(4);
			return uri;
		}

		public void AddResubscribe(string uri)
		{
			writer.WriteStartElement(@"resource");
			writer.WriteAttributeString(@"uri", uri);

			writer.WriteStartElement(@"instance");
			writer.WriteAttributeString(@"id", @"0");
			writer.WriteAttributeString(@"state", @"resubscribe");
			writer.WriteAttributeString(@"cid", GetCid(uri));
			writer.WriteEndElement();

			writer.WriteEndElement();
		}

		/*
		public void AddTerminated(string uri, uint statusCode, string reason)
		{
		}
		*/
	}
}

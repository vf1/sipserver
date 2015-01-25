using System;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Text;

namespace EnhancedPresence
{
	public abstract class OutContentBase
	{
		public abstract string OutContentType { get; }
		public abstract string OutContentSubtype { get; }
		public abstract void Generate(XmlWriter writer);
	}

	public class OutContent
		: OutContentBase
	{
		private Object sync;
		private OutContentBase outContent;

		public OutContent(OutContentBase outContent, Object sync)
		{
			this.outContent = outContent;
			this.sync = sync;
		}

		public override string ToString()
		{
			return this.GenerateToString();
		}

		public string GenerateToString()
		{
			StringBuilder stringBilder = new StringBuilder();

			XmlWriterSettings settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = true;

			using (XmlWriter writer = XmlWriter.Create(stringBilder, settings))
			{
				this.Generate(writer);
				writer.Flush();

				return stringBilder.ToString();
			}
		}

		public byte[] GenerateToByteArray()
		{
			var utf8 = new System.Text.UTF8Encoding(false);
			using (MemoryStream memoryStream = new MemoryStream())
			using (StreamWriter streamWriter = new StreamWriter(memoryStream, utf8))
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.OmitXmlDeclaration = true;

				using (XmlWriter writer = XmlWriter.Create(streamWriter, settings))
				{
					this.Generate(writer);
					writer.Flush();

					return memoryStream.ToArray();
				}
			}
		}

		public override string OutContentType
		{
			get
			{
				lock (sync)
					return outContent.OutContentType;
			}
		}

		public override string OutContentSubtype
		{
			get
			{
				lock (sync)
					return outContent.OutContentSubtype;
			}
		}

		public override void Generate(XmlWriter writer)
		{
			lock (sync)
			{
				try
				{
					outContent.Generate(writer);
				}
				catch (Exception e)
				{
					throw new EnhancedPresenceException("Generate failed", e);
				}
			}
		}
	}
}

using System;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections.Generic;

namespace EnhancedPresence
{
	public class UserPropertiesCategory
		: ICategoryValue
	{
		public List<Line> Lines { get; set; }
		public LineType? TelephonyMode { get; set; }
		public string FaxNumber { get; set; }
		public string StreetAddress { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string CountryCode { get; set; }
		public string PostalCode { get; set; }
		public string WwwHomePage { get; set; }

		public string Line1
		{
			get
			{
				return GetLine1().Value;
			}
			set
			{
				GetLine1().Value = value;
			}
		}

		public LineType LineType1
		{
			get
			{
				return GetLine1().LineType;
			}
			set
			{
				GetLine1().LineType = value;
			}
		}

		public string LineServer1
		{
			get
			{
				return GetLine1().LineServer;
			}
			set
			{
				GetLine1().LineServer = value;
			}
		}

		private Line GetLine1()
		{
			if (Lines == null)
				Lines = new List<Line>();
			if (Lines.Count < 1)
				Lines.Add(new Line());
			return Lines[0];
		}

		public void Generate(XmlWriter writer)
		{
			writer.WriteStartElement(@"userProperties");

			if (Lines != null)
			{
				writer.WriteStartElement(@"lines");

				foreach (Line line in Lines)
					line.Generate(writer);

				writer.WriteEndElement();
			}

			if (TelephonyMode != null)
				WriteTaggedValue(writer, @"telephonyMode", this.TelephonyMode.ToString());

			WriteTaggedValue(writer, @"facsimileTelephoneNumber", this.FaxNumber);
			WriteTaggedValue(writer, @"streetAddress", this.StreetAddress);
			WriteTaggedValue(writer, @"l", this.City);
			WriteTaggedValue(writer, @"st", this.State);
			WriteTaggedValue(writer, @"countryCode", this.CountryCode);
			WriteTaggedValue(writer, @"postalCode", this.PostalCode);
			WriteTaggedValue(writer, @"wWWHomePage", this.WwwHomePage);

			writer.WriteEndElement();

			writer.Flush();
		}

		private void WriteTaggedValue(XmlWriter writer, string tagName, string value)
		{
			if (string.IsNullOrEmpty(value) == false)
			{
				writer.WriteStartElement(tagName);
				writer.WriteValue(value);
				writer.WriteEndElement();
			}
		}

		public class Line
			: ICategoryValue
		{
			public string Value { get; set; }
			public LineType LineType { get; set; }
			public string LineServer { get; set; }

			public void Generate(XmlWriter writer)
			{
				if (string.IsNullOrEmpty(Value) == false)
				{
					writer.WriteStartElement(@"line");

					writer.WriteAttributeString(@"lineType", LineType.ToString());
					if (string.IsNullOrEmpty(LineServer) == false || LineType == LineType.Rcc)
						writer.WriteAttributeString(@"lineServer", LineServer);
					
					writer.WriteValue(Value);
					
					writer.WriteEndElement();
				}
			}
		}

		public enum LineType
		{
			Uc,
			Rcc,
			Dual
		}
	}
}

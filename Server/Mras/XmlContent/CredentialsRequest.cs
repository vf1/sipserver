using System;
using System.Xml.Schema;
using System.Xml;

namespace Mras.XmlContent
{
	#region Schemas
	//<!-- CREDENTIALS REQUEST TYPE-->
	//<xs:complexType name="credentialsRequestType">
	//    <xs:sequence>
	//        <xs:element name="identity" type="tns:max64kCharStringType" />
	//        <xs:element name="location" type="tns:locationType" minOccurs="0" />
	//        <xs:element name="duration" type="xs:positiveInteger" minOccurs="0" />
	//    </xs:sequence>
	//    <xs:attribute name="credentialsRequestID" type="tns:max64CharStringType" use="required" />
	//</xs:complexType>

	//<!--LOCATION TYPE-->
	//<xs:simpleType name="locationType">
	//	<xs:restriction base="xs:string">
	//		<xs:enumeration value="intranet"/>
	//		<xs:enumeration value="internet"/>
	//	</xs:restriction>
	//</xs:simpleType>
	#endregion

	public class CredentialsRequest
	{
		public string Identity { get; private set; }
		public Location? Location { get; private set; }
		public uint Duration { get; set; }
		public string CredentialsRequestID { get; private set; }

		public static bool CanParse(XmlReader reader)
		{
			return reader.IsStartElement(@"credentialsRequest");
		}

		public static CredentialsRequest Parse(XmlReader reader)
		{
			CredentialsRequest credentialsRequest = new CredentialsRequest();

			reader.MoveToContent();
			credentialsRequest.CredentialsRequestID = reader.GetAttribute(@"credentialsRequestID");

			reader.Read();
			reader.MoveToContent();
			credentialsRequest.Identity =
				reader.ReadElementContentAsString(@"identity", @"http://schemas.microsoft.com/2006/09/sip/mrasp");

			if (reader.IsStartElement(@"location"))
				credentialsRequest.Location = Common.ParseLocation(reader.ReadElementContentAsString());

			if (reader.IsStartElement(@"duration"))
				credentialsRequest.Duration = (uint)reader.ReadElementContentAsInt();

			reader.MoveToContent();
			reader.ReadEndElement();

			return credentialsRequest;
		}
	}
}

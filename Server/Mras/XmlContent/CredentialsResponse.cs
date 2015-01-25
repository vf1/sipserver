using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml;

namespace Mras.XmlContent
{
	#region Schemas
	//<!-- RESPONSE TYPE-->
	//<xs:complexType name="credentialsResponseType">
	//	<xs:sequence>
	//		<xs:element name="credentials" type="tns:credentialsType" />
	//		<xs:element name="mediaRelayList" type="tns:mediaRelayListType" />
	//	</xs:sequence>
	//	<xs:attribute name="credentialsRequestID" type="xs:string" use="required"/>
	//</xs:complexType>

	//<!--CREDENTIALS TYPE-->
	//<xs:complexType name="credentialsType">
	//	<xs:sequence>
	//		<xs:element name="username" type="xs:string" />
	//		<xs:element name="password" type="xs:string" />
	//		<xs:element name="duration" type="xs:positiveInteger" />
	//		<xs:element name="realm" type="xs:string" minOccurs="0"/>
	//	</xs:sequence>
	//</xs:complexType>

	//<!--MEDIA RELAY LIST TYPE-->
	//<xs:complexType name="mediaRelayListType">
	//	<xs:sequence>
	//		<xs:element name="mediaRelay" type="tns:mediaRelayType" minOccurs="1" maxOccurs="unbounded"/>
	//	</xs:sequence>
	//</xs:complexType>

	//<!--MEDIA RELAY TYPE-->
	//<xs:complexType name="mediaRelayType">
	//	<xs:sequence>
	//		<xs:element name="location" type="tns:locationType"/>
	//		<xs:choice>
	//			<xs:element name="hostName" type="tns:hostNameType"/>
	//			<xs:element name="directIPAddress" type="tns:max64CharStringType"/>
	//		</xs:choice>
	//		<xs:element name="udpPort" type="xs:unsignedShort" minOccurs="0"/>
	//		<xs:element name="tcpPort" type="xs:unsignedShort" minOccurs="0"/>
	//	</xs:sequence>
	//</xs:complexType>
	#endregion

	public class CredentialsResponse
	{
		public string CredentialsRequestID { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public uint Duration { get; set; }
		public string Realm { get; set; }
		public IEnumerable<MediaRelay> MediaRelays1 { get; set; }
		public IEnumerable<MediaRelay> MediaRelays2 { get; set; }

		public void Generate(XmlWriter writer)
		{
			writer.WriteStartElement(@"credentialsResponse");
			writer.WriteAttributeString(@"credentialsRequestID", CredentialsRequestID);


			writer.WriteStartElement(@"credentials");

			writer.WriteElementString(@"username", Username);
			writer.WriteElementString(@"password", Password);
			writer.WriteElementString(@"duration", Duration.ToString());
			if (string.IsNullOrEmpty(Realm) == false)
				writer.WriteElementString(@"realm", Realm);

			writer.WriteEndElement();


			writer.WriteStartElement(@"mediaRelayList");

			if (MediaRelays1 != null)
				foreach (var mediaRelay in MediaRelays1)
					mediaRelay.Generate(writer);
			if (MediaRelays2 != null)
				foreach (var mediaRelay in MediaRelays2)
					mediaRelay.Generate(writer);

			writer.WriteEndElement();


			writer.WriteEndElement();
		}
	}
}

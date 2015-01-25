using System;
using System.Xml.Schema;
using System.Xml;

namespace Mras.XmlContent
{
	#region Schemas
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

	public class MediaRelay
	{
		public Location Location { get; set; }
		public string HostName { get; set; }
		public string DirectIpAddress { get; set; }
		public ushort UdpPort { get; set; }
		public ushort TcpPort { get; set; }

		public void Generate(XmlWriter writer)
		{
			writer.WriteStartElement(@"mediaRelay");

			writer.WriteElementString(@"location", Common.ToString(Location));

			if (string.IsNullOrEmpty(HostName) == false)
				writer.WriteElementString(@"hostName", HostName);
			else
				writer.WriteElementString(@"directIPAddress", DirectIpAddress);

			writer.WriteElementString(@"udpPort", UdpPort.ToString());
			writer.WriteElementString(@"tcpPort", TcpPort.ToString());

			writer.WriteEndElement();
		}
	}
}

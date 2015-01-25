using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml;

namespace Mras.XmlContent
{
	public class Request
	{
		#region Schemas
		//<xs:complexType name="requestType">
		//    <xs:sequence>
		//        <!-- number of credentials requests will be bound within MRAS-->
		//        <xs:element name="credentialsRequest" type="tns:credentialsRequestType" minOccurs="1" maxOccurs="100"/>
		//    </xs:sequence>
		//    <xs:attribute name="requestID" type="tns:max64CharStringType" use="required"/>
		//    <xs:attribute name="version" type="tns:versionType" use="required"/>
		//    <xs:attribute name="to" type="tns:mrasUriType" use="required"/>
		//    <xs:attribute name="from" type="tns:mrasUriType" use="required"/>
		//    <xs:attribute name="route" type="tns:routeType" use="optional" default="loadbalanced"/>
		//</xs:complexType>

		//<!--ROUTE TYPE-->
		//<xs:simpleType name="routeType">
		//	<xs:restriction base="xs:string">
		//		<xs:enumeration value="loadbalanced" />
		//		<xs:enumeration value="directip" />
		//	</xs:restriction>
		//</xs:simpleType>
		#endregion

		protected Request()
		{
		}

		public List<CredentialsRequest> CredentialsRequests { get; private set; }
		public string RequestId { get; private set; }
		public Version Version { get; private set; }
		public string To { get; private set; }
		public string From { get; private set; }
		public Route Route { get; private set; }

		public static Request Parse(XmlReader reader)
		{
			try
			{
				Request request = new Request();

				if (reader.IsStartElement(@"request") == false)
					throw new MrasException();

				request.RequestId = reader.GetAttribute(@"requestID");
				request.Version = reader.GetAttribute(@"version");
				request.To = reader.GetAttribute(@"to");
				request.From = reader.GetAttribute(@"from");
				request.Route = Common.ParseRoute(reader.GetAttribute(@"route"));

				request.CredentialsRequests = new List<CredentialsRequest>();

				reader.Read();
				while (CredentialsRequest.CanParse(reader))
					request.CredentialsRequests.Add(CredentialsRequest.Parse(reader));

				if (request.CredentialsRequests.Count <= 0)
					throw new MrasException();
				if (request.CredentialsRequests.Count > 100)
					throw new MrasException(ReasonPhrase.RequestTooLarge);

				return request;

			}
			catch (XmlException ex)
			{
				throw WrapException(ex);
			}
			catch (InvalidOperationException ex)
			{
				throw WrapException(ex);
			}
			catch (ArgumentException ex)
			{
				throw WrapException(ex);
			}
			catch (Exception)
			{
				throw;
			}
		}

		private static Exception WrapException(Exception exception)
		{
			if (exception.Source == @"System.Xml")
				return new MrasException(exception);
			
			return exception;
		}
	}
}

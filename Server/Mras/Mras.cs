using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Mras.XmlContent;
using EnhancedPresence;

namespace Mras
{
	public class Mras1
	{
		private Object sync;
		private byte[] key0;

		public Mras1()
		{
			sync = new Object();
			key0 = new byte[] { 
				1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 
				1, 2, 3, 4, 5, 6, 7, 8, 9, 20, 
				1, 2, 3, 4, 5, 6, 7, 8, 9, 30, 
				1, 2, 3, 4, 5, 6, 7, 8, 9, 40, 
				1, 2, 3, 4, 5, 6, 7, 8, 9, 50, 
				1, 2, 3, 4, 5, 6, 7, 8, 9, 60, 
				1, 2, 3, 4 };

			Duration = 480;
			//TurnServers = new SynchronizedCollection<TurnServerInfo>(sync);
			internetServers = new List<TurnServerInfo>();
			intranetServers = new List<TurnServerInfo>();

//#if DEBUG
//            Key1 = new byte[] {
//                1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,
//            };
//            Key2 = new byte[] {
//                1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0,
//            };
//            TurnServers.Clear();
//            TurnServers.Add(new TurnServerInfo()
//            {
//                Internet = new TurnServerInfo.Interface()
//                {
//                    Fqdn = @"192.168.1.15",
//                    //Fqdn = @"192.168.56.101", // VirtualBox
//                    TcpPort = 3478,
//                    UdpPort = 3478,
//                },
//                Intranet = new TurnServerInfo.Interface()
//                {
//                   Fqdn = @"192.168.1.15",
//                   //Fqdn = @"192.168.56.101", // VirtualBox
//                   TcpPort = 3478,
//                   UdpPort = 3478,
//                },
//            });
//#endif
		}

		public byte[] Key1 { get; set; }
		public byte[] Key2 { get; set; }
		public uint Duration { get; set; }
		private List<TurnServerInfo> internetServers;
		private List<TurnServerInfo> intranetServers;

		public void SetKeys(byte[] key1, byte[] key2)
		{
			lock (sync)
			{
				Key1 = key1;
				Key2 = key2;
			}
		}

		public void ClearServers()
		{
			lock (sync)
			{
				internetServers.Clear();
				intranetServers.Clear();
			}
		}

		public void AddInternetServer(string fqdn, int tcpPort, int udpPort)
		{
			internetServers.Add(new TurnServerInfo() { Fqdn = fqdn, TcpPort = (ushort)tcpPort, UdpPort = (ushort)udpPort });
		}

		public void AddIntranetServer(string fqdn, int tcpPort, int udpPort)
		{
			intranetServers.Add(new TurnServerInfo() { Fqdn = fqdn, TcpPort = (ushort)tcpPort, UdpPort = (ushort)udpPort });
		}

		//public void Set(string fqdn, int tcpPort, int udpPort, byte[] key1, byte[] key2)
		//{
		//    lock (sync)
		//    {
		//        Key1 = key1;
		//        Key2 = key2;

		//        TurnServers.Clear();

		//        if (String.IsNullOrEmpty(fqdn) == false)
		//        {
		//            TurnServers.Add(new TurnServerInfo()
		//            {
		//                Internet = new TurnServerInfo.Interface()
		//                {
		//                    Fqdn = fqdn,
		//                    TcpPort = (ushort)tcpPort,
		//                    UdpPort = (ushort)udpPort,
		//                },
		//                Intranet = new TurnServerInfo.Interface()
		//                {
		//                    Fqdn = fqdn,
		//                    TcpPort = (ushort)tcpPort,
		//                    UdpPort = (ushort)udpPort,
		//                },
		//            });
		//        }
		//    }
		//}

		public OutContent ProcessRequest(ArraySegment<byte> content)
		{
			lock (sync)
			{
				Request request = null;

				try
				{
					if (Key1 == null || Key2 == null)
						throw new MrasException(ReasonPhrase.NotSupported);

					if (content == null)
						throw new MrasException(ReasonPhrase.RequestMalformed);

					using (var xmlReader = CreateXmlReader(content))
						request = Request.Parse(xmlReader);

					if ((request.Version.Major != 1 && request.Version.Major != 2) || request.Version.Minor != 0)
						throw new MrasException(ReasonPhrase.VersionMismatch);

					// The from and to attributes MUST be SIP URIs.
					// ReasonPhrase.RequestMalformed

#if DEBUG
					if (request.From == "sip:jdoe7@officesip.local")
						throw new MrasException(ReasonPhrase.NotSupported);
#endif

					return new OutContent(new Response()
					{
						Request = request,
						CredentialsResponses = request.CredentialsRequests.Select<CredentialsRequest, CredentialsResponse>(
							credentialsRequest => { return Process(credentialsRequest, request.Version.Major); }),
						ServerVersion = request.Version,
						ReasonPhrase = ReasonPhrase.OK,
					}, this.sync);
				}
				catch (MrasException ex)
				{
					return new OutContent(new Response()
					{
						Request = request,
						ReasonPhrase = ex.ReasonPhrase,
						ServerVersion = (request != null) ? request.Version : XmlContent.Version.V1,
					}, this.sync);
				}
				//catch (Exception)
				//{
				//    return new Response()
				//    {
				//        Request = request,
				//        ReasonPhrase = ReasonPhrase.InternalServerError,
				//        ServerVersion = ServerVersion,
				//    };
				//}
			}
		}

		private CredentialsResponse Process(CredentialsRequest credentialsRequest, int versionMajor)
		{
			TokenBlob1 tokenBlob = (versionMajor == 1) ? new TokenBlob1() : new TokenBlob2();

			using (HMACSHA1 sha1 = new HMACSHA1(key0))
			{
				UTF8Encoding utf8 = new UTF8Encoding();
				tokenBlob.ClientID = sha1.ComputeHash(utf8.GetBytes(credentialsRequest.Identity));
			}

			byte[] username = Username.GetBytes(Key1, tokenBlob);
			byte[] password = Password.GetBytes(Key2, username);

			IEnumerable<MediaRelay> mediaRelays1 = null, mediaRelays2 = null;

			if (credentialsRequest.Location == null || credentialsRequest.Location == Location.Intranet)
				mediaRelays2 = intranetServers.Select<TurnServerInfo, MediaRelay>(
					turnServer => new MediaRelay()
					{
						Location = Location.Intranet,
						HostName = turnServer.Fqdn,
						TcpPort = turnServer.TcpPort,
						UdpPort = turnServer.UdpPort,
					});

			if (credentialsRequest.Location == null || credentialsRequest.Location == Location.Internet)
				mediaRelays2 = internetServers.Select <TurnServerInfo, MediaRelay>(
					turnServer => new MediaRelay()
					{
						Location = Location.Internet,
						HostName = turnServer.Fqdn,
						TcpPort = turnServer.TcpPort,
						UdpPort = turnServer.UdpPort,
					});


			return new CredentialsResponse()
			{
				CredentialsRequestID = credentialsRequest.CredentialsRequestID,
				Duration = Math.Min(credentialsRequest.Duration, Duration),
				Username = Convert.ToBase64String(username),
				Password = Convert.ToBase64String(password),
				MediaRelays1 = mediaRelays1,
				MediaRelays2 = mediaRelays2,
			};
		}

		private XmlReader CreateXmlReader(ArraySegment<byte> content)
		{
			MemoryStream memoryStream = new MemoryStream(content.Array, content.Offset, content.Count);
			StreamReader streamReader = new StreamReader(memoryStream, System.Text.Encoding.UTF8);

			return XmlReader.Create(streamReader);
		}
	}
}

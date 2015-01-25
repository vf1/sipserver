using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using Sip.Server;
using Sip.Simple;
using Sip.Server.Configuration;
using Sip.Server.Users;
using Sip.Server.Accounts;
using Sip.Server.WcfService;
//using Sip.Message;
//using Http.Message;
using SocketServers;
using Server.Xcap;
using Server.Http;
using Server.Restapi;
using Server.Authorization;
using Server.Authorization.Sip;
using Server.Authorization.Http;
using Server.Configuration;
using SipAuthSchemes = Sip.Message.AuthSchemes;
using HttpAuthSchemes = Http.Message.AuthSchemes;

namespace Sip.Server
{
	class Initializer
	{
		public delegate void GetResultsDelegate(
			out TransportLayer transportLayer,
			out TransactionLayer transactionLayer,
			out LocationService locationService,
			out WCFService wcfService,
			out ConfigurationMonitor configurationMonitor,
			out TrunkManager trunkManager,
			out SipAuthorizationManager authorization,
			out Userz userz,
			out AdUsers adUsers,
			out Mras.Mras1 mras,
			out HttpFileServer httpFileServer,
			out Accountx accounts,
			out RestapiService restapi,
            out ProxyServerTU proxyServerTU
			);

		public GetResultsDelegate GetResults;

		public Initializer(EventHandler<EventArgs> configChangedHandler)
		{
			var configuration = SipServerConfigurationSection.GetSection();

			var adUsers = (configuration.IsActiveDirectoryEnabled == false) ? null : new AdUsers(configuration.ActiveDirectoryGroup);

			Func<Userz> CreateUserz = () =>
			{
				var result = new Userz(configuration.CustomUsersPath);

				result.Add(new CsvUsers(configuration.UsersCsvFilePathName));
				if (adUsers != null)
					result.Add(adUsers);

				result.LoadCustomUsers();

				return result;
			};


			var serversManager = new ServersManager<Connection>(new ServersManagerConfig());
			var transportLayer = new TransportLayer(serversManager, configuration.WebSocketResponseFrame);
			var authorization = new SipAuthorizationManager();
			var transactionLayer = new TransactionLayer(authorization);
			var userz = CreateUserz();
			var locationService = new LocationService();
			var mras = new Mras.Mras1();
			var trunkManager = new TrunkManager();
			var accounts = new Accountx(configuration.AccountConfigFilePathName);
			var msPresTu = new MsPresTU(accounts, userz, locationService);
			var wcfService = new WCFService(configuration, msPresTu.EnhancedPresence, trunkManager, accounts, userz);
			var httpAuthorization = new HttpAuthorizationManager();
			var httpServer = new HttpServer(httpAuthorization, configuration.AdminUri);
			//var httpServerAgentRegistrar = new Func<IHttpServerAgent, IHttpServer>((agent) => { return httpServer.Register(agent); });
			var restapi = new RestapiService(accounts, userz) { AdministratorPassword = configuration.AdministratorPassword, };
			var httpFileServer = new HttpFileServer(configuration.WwwPath, string.Empty);
			var xcapServer = new XcapServer();
			var configurationMonitor = new ConfigurationMonitor();
			var simpleModule = new SimpleModule(EqualityComparer<string>.Default);
            var proxyServerTU = new ProxyServerTU(locationService, trunkManager, accounts);

			GetResults = (
				out TransportLayer transportLayer1,
				out TransactionLayer transactionLayer1,
				out LocationService locationService1,
				out WCFService wcfService1,
				out ConfigurationMonitor configurationMonitor1,
				out TrunkManager trunkManager1,
				out SipAuthorizationManager authorization1,
				out Userz userz1,
				out AdUsers adUsers1,
				out Mras.Mras1 mras1,
				out HttpFileServer httpFileServer1,
				out Accountx accounts1,
				out RestapiService restapi1,
                out ProxyServerTU proxyServerTU1
				) =>
			{
				transportLayer1 = transportLayer;
				transactionLayer1 = transactionLayer;
				locationService1 = locationService;
				wcfService1 = wcfService;
				configurationMonitor1 = configurationMonitor;
				trunkManager1 = trunkManager;
				authorization1 = authorization;
				userz1 = userz;
				adUsers1 = adUsers;
				mras1 = mras;
				httpFileServer1 = httpFileServer;
				accounts1 = accounts;
				restapi1 = restapi;
                proxyServerTU1 = proxyServerTU;
			};


			Action InitializeTracer = () =>
			{
				Tracer.Initialize(serversManager.Logger);
                Tracer.Configure(configuration.TracingPath, configuration.IsTracingEnabled);
			};


			Action InitializeConfigurationMonitor = () =>
			{
				configurationMonitor.Changed += configChangedHandler;
				configurationMonitor.StartMonitoring(configuration);
			};

			Action InitializeHttpModules = () =>
			{
				httpServer.SendAsync = transportLayer.SendAsyncHttp;
				transportLayer.IncomingHttpRequest = httpServer.ProcessIncomingRequest;

				httpServer.Register(restapi, 0, true);
				httpServer.Register(xcapServer, 0, true);
				httpServer.Register(new HttpFileServer(configuration.WwwAdminPath, configuration.AdminUri), 254, true);
				httpServer.Register(httpFileServer, 255, true);

				xcapServer.AddHandler(new ResourceListsHandler(accounts, userz));
				xcapServer.AddHandler(new PidfManipulationHandler(simpleModule));
			};

			Action InitializeWcfService = () =>
			{
				wcfService.Start();
			};


            Action InitializeProxyServerTU = () =>
            {
                proxyServerTU.IsOfficeSIPFiletransferEnabled = configuration.IsOfficeSIPFiletransferEnabled;
            };


            Action InitializeTransactionLayer = () =>
			{
                InitializeProxyServerTU();

				transportLayer.IncomingMessage += transactionLayer.IncomingMessage;
				transportLayer.SendErrorSip += transactionLayer.TransportError;
				transactionLayer.SendAsync = transportLayer.SendAsyncSip;
				transactionLayer.IsLocalAddress = transportLayer.IsLocalAddress;

				serversManager.EndConnection += (s, c) => { locationService.RemoveBindingsWhenConnectionEnd(c.Id); };
				transactionLayer.RegisterTransactionUser(new RegistrarTU(locationService, accounts));

				transactionLayer.RegisterTransactionUser(msPresTu);

				transactionLayer.RegisterTransactionUser(new SimpleTU(simpleModule));
				transactionLayer.RegisterTransactionUser(new OptionsTU());
				transactionLayer.RegisterTransactionUser(new MessageSummaryTU());

				transactionLayer.RegisterTransactionUser(new MrasTU(mras));

				transactionLayer.RegisterTransactionUser(new DirectorySearchTU(accounts, new ServiceSoap.ServiceSoap1(), userz));

                transactionLayer.RegisterTransactionUser(proxyServerTU);

				transactionLayer.RegisterTransactionUser(new TrunkTU(trunkManager));
				transactionLayer.RegisterTransactionUser(new ErrorTU());
			};


			Action InitializeAuthorization = () =>
			{
				authorization.IsEnabled = configuration.IsAuthorizationEnabled;

				if (configuration.IsActiveDirectoryEnabled)
				{
					var kerberosAuth = new SipMicrosoftAuthentication(SipAuthSchemes.Kerberos, accounts, userz);
					var ntlmAuth = new SipMicrosoftAuthentication(SipAuthSchemes.Ntlm, accounts, userz);

					authorization.RegisterAgent(kerberosAuth, SipAuthSchemes.Kerberos);
					authorization.RegisterAgent(ntlmAuth, SipAuthSchemes.Ntlm);
				}

				var digestAuth = new SipDigestAuthentication(accounts, userz, configuration.IsAuthIntEnabled);
				authorization.RegisterAgent(digestAuth, SipAuthSchemes.Digest);



				httpAuthorization.IsEnabled = configuration.IsAuthorizationEnabled;
				httpAuthorization.RegisterAgent(new HttpDigestAuthentication(accounts, userz, false), HttpAuthSchemes.Digest);
			};


			Action InitializeServersManager = () =>
			{
				serversManager.FakeAddressAction = (ServerEndPoint localEndpoint) =>
				{
					foreach (var portForwarding in configuration.PortForwardings)
					{
						if (localEndpoint.Equals(portForwarding.Protocol, portForwarding.LocalEndpoint))
							return portForwarding.ExternalEndpoint;
					}

					return null;
				};

				Action<ServerProtocol, int> bind = (protocol, port) =>
				{
					if (port > 0)
					{
						var error = serversManager.Bind(new ProtocolPort()
						{
							Protocol = protocol,
							Port = port,
						});

						if (error != SocketError.Success)
							Tracer.WriteError("Can't open " + protocol + " port " + port + ".\r\n" + error.ToString());
					}
				};

				if (configuration.UdpPort > 0)
					bind(ServerProtocol.Udp, configuration.UdpPort);
				if (configuration.TcpPort > 0)
					bind(ServerProtocol.Tcp, configuration.TcpPort);
				if (configuration.TcpPort2 > 0)
					bind(ServerProtocol.Tcp, configuration.TcpPort2);
			};

			ConfigureMras(mras, configuration);
			InitializeConfigurationMonitor();
			InitializeTracer();
			InitializeTransactionLayer();
			InitializeAuthorization();
			InitializeWcfService();
			InitializeHttpModules();
			InitializeServersManager();
		}

		private enum HttpModuleId : int
		{
			HttpServer,
			XcapServer,
		}

		public static void ConfigureMras(Mras.Mras1 mras, SipServerConfigurationSection configuration)
		{
			mras.SetKeys(configuration.TurnServers.Key1, configuration.TurnServers.Key2);
			mras.ClearServers();
			foreach (TurnServerConfigurationElement server in configuration.TurnServers)
			{
				if (string.Compare(server.Location, "internet", true) == 0)
					mras.AddInternetServer(server.Fqdn, server.TcpPort, server.UdpPort);
				else
					mras.AddIntranetServer(server.Fqdn, server.TcpPort, server.UdpPort);
			}
		}

		public static void ConfigureVoipProviders(TrunkManager trunkManager, SipServerConfigurationSection configuration)
		{
			foreach (var provider in configuration.VoipProviders)
			{
				trunkManager.Clear();

				trunkManager.Add(
					new Trunk(
						provider.DisplayName,
						provider.ServerHostname,
						provider.Username,
						provider.Protocol.ToTransport(),
						provider.LocalEndpoint,
						provider.OutboundProxyHostname,
						provider.AuthenticationId,
						provider.Password,
						provider.ForwardIncomingCallTo,
						provider.RestoreAfterErrorTimeout));

				break; // one voip provider only!
			}
		}
	}
}

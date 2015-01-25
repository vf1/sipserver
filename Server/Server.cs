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
using Sip.Server.Configuration;
using Sip.Server.Users;
using Sip.Server.Accounts;
using Sip.Server.WcfService;
using Sip.Message;
using Http.Message;
using SocketServers;
using Server.Http;
using Server.Restapi;
using Server.Authorization.Sip;
using Server.Xcap;
using Server.Configuration;

namespace Sip.Server
{
    class Server
        : IDisposable
    {
#if !DEBUG
		private readonly CrashHandler crashHandler;
#endif
        private readonly TransportLayer transportLayer;
        private readonly TransactionLayer transactionLayer;
        private readonly LocationService locationService;
        private readonly WCFService wcfService;
        private readonly ConfigurationMonitor configurationMonitor;
        private readonly TrunkManager trunkManager;
        private readonly SipAuthorizationManager authorization;
        private readonly Userz userz;
        private readonly AdUsers adUsers;
        private readonly Mras.Mras1 mras;
        private readonly HttpFileServer httpServer;
        private readonly Accountx accounts;
        private readonly RestapiService restapi;
        private readonly ProxyServerTU proxyServerTU;

        public Server()
        {
#if !DEBUG
			crashHandler = new CrashHandler();
#endif
            HttpMessage.BufferManager = SipMessage.BufferManager = new BufferManagerProxy();

            LoadConfiguration();

            var configuration = SipServerConfigurationSection.GetSection();

            if (configuration.AddToWindowsFirewall)
                AddFirewallException();

            if (BufferManager.IsInitialized() == false)
                BufferManager.Initialize(Math.Min((int)(GetTotalMemoryInBytes() / (1024 * 1024) / 2), 2048));

            if (Directory.Exists(configuration.AccountsPath) == false)
                Directory.CreateDirectory(configuration.AccountsPath);

            var initializer = new Initializer(ConfigurationMonitor_Changed);

            initializer.GetResults(
                out transportLayer,
                out transactionLayer,
                out locationService,
                out wcfService,
                out configurationMonitor,
                out trunkManager,
                out authorization,
                out userz,
                out adUsers,
                out mras,
                out httpServer,
                out accounts,
                out restapi,
                out proxyServerTU);

            if (configuration.IsActiveDirectoryEnabled)
            {
                accounts.ForEach((account) =>
                    {
                        SetSpn(@"sip/" + account.DomainName);
                    });
            }

            RestapiUriParser.LoadTables(configuration.ExePath);
            XcapUriParser.LoadTables(configuration.ExePath);

            Http.Message.HttpMessageReader.InitializeAsync(
                (ms1) =>
                {
                    Sip.Message.SipMessageReader.InitializeAsync(
                        (ms2) =>
                        {
                            Tracer.WriteImportant(@"JIT-compilation Http.Message.dll " + (ms1 / 1000).ToString() + ", Sip.Message.dll: " + (ms2 / 1000).ToString() + " seconds.");

                            try
                            {
                                transportLayer.Start();
                                Tracer.WriteImportant(@"Server started.");
                            }
                            catch (Exception ex)
                            {
                                Tracer.WriteException(@"Failed to start Servers Manager.", ex);
                            }

                            Initializer.ConfigureVoipProviders(trunkManager, configuration);
                        });
                });
        }

        public void Dispose()
        {
            Dispose(
                configurationMonitor,
                accounts,
                userz,
                wcfService,
                locationService,
                transportLayer,
                httpServer);
        }

        public void Dispose(params IDisposable[] disposables)
        {
            foreach (var obj in disposables)
                try
                {
                    obj.Dispose();
                }
                catch
                {
                }
        }

        #region LoadConfiguration, ConfigurationMonitor_Changed

        private void LoadConfiguration()
        {
            var errors = SipServerConfigurationSection.LoadSection();

            if (errors.Count > 0)
            {
                // log errors!

                try
                {
                    var name = SipServerConfigurationSection.GetSection().FilePath;
                    var oldName = name + ".old";
                    File.Delete(oldName);
                    File.Move(name, oldName);
                    File.Delete(name);

                    var errors2 = SipServerConfigurationSection.LoadSection();
                    if (errors2.Count > 0)
                        throw errors2[0];
                }
                catch (Exception ex)
                {
                    throw new InvalidProgramException(@"Can not restore .config file", ex);
                }
            }
        }

        private void ConfigurationMonitor_Changed(object sender, EventArgs e)
        {
            var errors = SipServerConfigurationSection.LoadSection();

            if (errors.Count > 0)
            {
                // log errors!
            }
            else
            {
                var configuration = SipServerConfigurationSection.GetSection();

                Tracer.Configure(configuration.TracingPath, configuration.IsTracingEnabled);

                wcfService.AdministratorPassword = configuration.AdministratorPassword;
                restapi.AdministratorPassword = configuration.AdministratorPassword;

                if (adUsers != null)
                    adUsers.Group = configuration.ActiveDirectoryGroup;

                authorization.IsEnabled = configuration.IsAuthorizationEnabled;

                trunkManager.Clear();
                Initializer.ConfigureVoipProviders(trunkManager, configuration);

                Initializer.ConfigureMras(mras, configuration);

                httpServer.WwwPath = configuration.WwwPath;

                transportLayer.ChangeSettings(configuration.WebSocketResponseFrame);

                proxyServerTU.IsOfficeSIPFiletransferEnabled = configuration.IsOfficeSIPFiletransferEnabled;
            }
        }

        #endregion

        #region SetSpn

        private void SetSpn(string spn)
        {
            try
            {
                using (var entry = UserPrincipal.Current.GetUnderlyingObject() as DirectoryEntry)
                {
                    var servicePrincipalNames = entry.Properties["servicePrincipalName"];

                    if (servicePrincipalNames.IndexOf(spn) < 0)
                    {
                        servicePrincipalNames.Add(spn);
                        entry.CommitChanges();
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region AddFirewallException, GetAssemblyTitle

        private static void AddFirewallException()
        {
            try
            {
                using (var p = new Process())
                {
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.FileName = "netsh";

                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                    p.StartInfo.Arguments = string.Format("firewall add allowedprogram \"{0}\" \"{1}\" ENABLE",
                        assembly.Location, GetAssemblyTitle(assembly));

                    p.Start();
                    p.WaitForExit(5000);
                }
            }
            catch
            {
            }
        }

        private static string GetAssemblyTitle(System.Reflection.Assembly assembly)
        {
            var attributes = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                var attribute = attributes[0] as System.Reflection.AssemblyTitleAttribute;
                if (attribute != null)
                    return attribute.Title;
            }

            throw new InvalidProgramException(@"Can not retrive Title, assembly attribute");
        }

        #endregion

        #region GetTotalMemoryInBytes

        private static ulong GetTotalMemoryInBytes()
        {
            try
            {
                return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            }
            catch
            {
                return 1024 * 1024 * 1024;
            }
        }

        #endregion
    }
}

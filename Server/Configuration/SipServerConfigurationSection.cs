using System;
using System.IO;
using System.Xml;
using System.Configuration;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Server.Http;
using Server.Configuration;

namespace Sip.Server.Configuration
{
    class SipServerConfigurationSection
        : ConfigurationSectionEx
    {
        private const string udpPortName = @"udpPort";
        private const string tcpPortName = @"tcpPort";
        private const string tcpPort2Name = @"tcpPort2";
        private const string isAuthorizationEnabledName = @"isAuthorizationEnabled";
        private const string isAuthIntEnabledName = @"isAuthIntEnabled";
        private const string isTracingEnabledName = @"isTracingEnabled";
        private const string tracingPathName = @"tracingPath";
        private const string wcfServiceAddressName = @"wcfServiceAddress";
        private const string administratorPasswordName = @"administratorPassword";
        private const string activeDirectoryGroupName = @"activeDirectoryGroup";
        private const string isActiveDirectoryEnabledName = @"isActiveDirectoryEnabled";
        private const string wwwPathName = @"wwwPath";
        private const string turnServersName = @"turnServers";
        private const string portForwardingsName = @"portForwardings";
        private const string voipProvidersName = @"voipProviders";
        private const string addToWindowsFirewallName = @"addToWindowsFirewall";
        private const string webSocketResponseFrameName = @"webSocketResponseFrame";
        private const string isOfficeSIPFiletransferEnabledName = @"isOfficeSIPFiletransferEnabled";

        public const string DefaultCongfigFileName = "Sip.Server.config";
        public const string DefaultCsvFileName = "Users.csv";
        public const string DefaultAccountConfigFileName = "Account.config";

        private readonly static string applicationDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\OfficeSIP\Server\";

        private readonly static string exePath =
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public SipServerConfigurationSection()
            : base(@"sipServer", applicationDataPath + DefaultCongfigFileName)
        {
        }

        public string ApplicationDataPath
        {
            get { return applicationDataPath; }
        }

        public string ExePath
        {
            get { return exePath; }
        }

        public string AccountsPath
        {
            get { return ApplicationDataPath + @"Accounts\"; }
        }

        public string UsersCsvFilePathName
        {
            get { return ApplicationDataPath + @"Accounts\{account-id}\" + DefaultCsvFileName; }
        }

        public string AccountConfigFilePathName
        {
            get { return ApplicationDataPath + @"Accounts\{account-id}\" + DefaultAccountConfigFileName; }
        }

        public string DefaultTracingPath
        {
            get { return ApplicationDataPath + @"Tracing\"; }
        }

        public string CustomUsersPath
        {
            get { return ExePath + @"\Users"; }
        }

        public string WwwAdminPath
        {
            get { return ExePath + @"\Www"; }
        }

        public string AdminUri
        {
            get { return @"/admin/"; }
        }

        [ConfigurationProperty(wwwPathName, DefaultValue = @"")]
        public string WwwPath
        {
            get { return (string)base[wwwPathName]; }
            set { base[wwwPathName] = value; }
        }

        [ConfigurationProperty(udpPortName, DefaultValue = 5060)]
        [IntegerValidator(MinValue = 0, MaxValue = 65535)]
        public int UdpPort
        {
            get { return (int)base[udpPortName]; }
            set { base[udpPortName] = value; }
        }

        [ConfigurationProperty(tcpPortName, DefaultValue = 5060)]
        [IntegerValidator(MinValue = 0, MaxValue = 65535)]
        public int TcpPort
        {
            get { return (int)base[tcpPortName]; }
            set { base[tcpPortName] = value; }
        }

        [ConfigurationProperty(tcpPort2Name, DefaultValue = 0)]
        [IntegerValidator(MinValue = 0, MaxValue = 65535)]
        public int TcpPort2
        {
            get { return (int)base[tcpPort2Name]; }
            set { base[tcpPort2Name] = value; }
        }

        [ConfigurationProperty(isAuthorizationEnabledName, DefaultValue = true)]
        public bool IsAuthorizationEnabled
        {
            get { return (bool)base[isAuthorizationEnabledName]; }
            set { base[isAuthorizationEnabledName] = value; }
        }

        [ConfigurationProperty(isAuthIntEnabledName, DefaultValue = false)]
        public bool IsAuthIntEnabled
        {
            get { return (bool)base[isAuthIntEnabledName]; }
            set { base[isAuthIntEnabledName] = value; }
        }

        [ConfigurationProperty(isTracingEnabledName, DefaultValue = false)]
        public bool IsTracingEnabled
        {
            get { return (bool)base[isTracingEnabledName]; }
            set { base[isTracingEnabledName] = value; }
        }

        [ConfigurationProperty(tracingPathName)]
        public string TracingPath
        {
            get
            {
                var stored = (string)base[tracingPathName];
                return string.IsNullOrEmpty(stored) ? DefaultTracingPath : stored;
            }
            set { base[tracingPathName] = value; }
        }

        [ConfigurationProperty(wcfServiceAddressName, DefaultValue = @"net.tcp://localhost:10001/officesip")]
        [RegexStringValidator(@"^net.tcp://.*")]
        public string WcfServiceAddress
        {
            get { return (string)base[wcfServiceAddressName]; }
            set { base[wcfServiceAddressName] = value; }
        }

        // "d41d8cd98f00b204e9800998ecf8427e"
        [ConfigurationProperty(administratorPasswordName, DefaultValue = @"6b5161794fec9e9782086636ae11398c")]
        public string AdministratorPassword
        {
            get { return (string)base[administratorPasswordName]; }
            set { base[administratorPasswordName] = value; }
        }

        [ConfigurationProperty(isActiveDirectoryEnabledName, DefaultValue = false)]
        public bool IsActiveDirectoryEnabled
        {
            get { return (bool)base[isActiveDirectoryEnabledName]; }
            set { base[isActiveDirectoryEnabledName] = value; }
        }

        [ConfigurationProperty(activeDirectoryGroupName, DefaultValue = @"")]
        public string ActiveDirectoryGroup
        {
            get { return (string)base[activeDirectoryGroupName]; }
            set { base[activeDirectoryGroupName] = value; }
        }

        [ConfigurationProperty(webSocketResponseFrameName, DefaultValue = @"Binary")]
        [RegexStringValidator("Binary|Text|AsRequest")]
        public string WebSocketResponseFrameString
        {
            get { return (string)base[webSocketResponseFrameName]; }
            set { base[webSocketResponseFrameName] = value; }
        }

        public Opcodes? WebSocketResponseFrame
        {
            get
            {
                if (WebSocketResponseFrameString.Equals("Binary", StringComparison.OrdinalIgnoreCase))
                    return Opcodes.Binary;
                if (WebSocketResponseFrameString.Equals("Text", StringComparison.OrdinalIgnoreCase))
                    return Opcodes.Text;
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    switch (value.Value)
                    {
                        case Opcodes.Binary:
                            WebSocketResponseFrameString = "Binary";
                            break;
                        case Opcodes.Text:
                            WebSocketResponseFrameString = "Text";
                            break;
                        default:
                            throw new InvalidEnumArgumentException("value", (int)value, typeof(Opcodes));
                    }
                }
                else
                {
                    WebSocketResponseFrameString = "AsRequest";
                }
            }
        }

        [ConfigurationProperty(turnServersName)]
        public TurnServerConfigurationElementCollection TurnServers
        {
            get { return base[turnServersName] as TurnServerConfigurationElementCollection; }
        }

        [ConfigurationProperty(portForwardingsName)]
        public PortForwardingConfigurationElementCollection PortForwardings
        {
            get { return base[portForwardingsName] as PortForwardingConfigurationElementCollection; }
        }

        [ConfigurationProperty(voipProvidersName)]
        public VoipProviderConfigurationElementCollection VoipProviders
        {
            get { return base[voipProvidersName] as VoipProviderConfigurationElementCollection; }
        }

        [ConfigurationProperty(addToWindowsFirewallName, DefaultValue = true)]
        public bool AddToWindowsFirewall
        {
            get { return (bool)base[addToWindowsFirewallName]; }
            set { base[addToWindowsFirewallName] = value; }
        }

        [ConfigurationProperty(isOfficeSIPFiletransferEnabledName, DefaultValue = true)]
        public bool IsOfficeSIPFiletransferEnabled
        {
            get { return (bool)base[isOfficeSIPFiletransferEnabledName]; }
            set { base[isOfficeSIPFiletransferEnabledName] = value; }
        }

        // ------------------------------------------------------------------------------------------

        public IList<Exception> Validate(string xml)
        {
            return ConfigurationSectionEx.Validate<SipServerConfigurationSection>(xml);
        }

        private volatile static SipServerConfigurationSection section = new SipServerConfigurationSection();

        public static IList<Exception> LoadSection()
        {
            var newSection = new SipServerConfigurationSection();
            newSection.Load();

            var errors = newSection.ListErrors();

            if (errors.Count == 0)
                System.Threading.Interlocked.Exchange<SipServerConfigurationSection>(ref section, newSection);

            return errors;
        }

        public static SipServerConfigurationSection GetSection()
        {
            return section;
        }
    }
}

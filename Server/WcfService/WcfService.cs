
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Selectors;
using EnhancedPresence;
using Sip.Server.Configuration;
using System.Configuration;
using Sip.Server;
using Sip.Server.Accounts;
using Sip.Server.Users;
using Sip.Server.WcfService;

namespace Sip.Server.WcfService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    sealed class WCFService : IWcfService, IDisposable
    {
        private ServiceHost serviceHost;
        private string serviceAddress;
        //private string domainName;
        private IUserz userz;
        private IWcfServiceCallback callback;
        private EnhancedPresence1 enhancedPresence;
        private TrunkManager trunkManager;
        private readonly CustomUserNamePasswordValidator validator;
        private readonly Accountx accounts;

        public WCFService(SipServerConfigurationSection configuration, EnhancedPresence1 enhancedPresence, TrunkManager trunkManager, Accountx accounts, IUserz userz)
        {
            this.accounts = accounts;

            this.userz = userz;
            this.userz.Reset += IUsers_Reloaded;
            this.userz.Added += IUsers_Added;
            this.userz.Updated += IUsers_Updated;
            this.userz.Removed += IUsers_Removed;

            this.enhancedPresence = enhancedPresence;
            this.enhancedPresence.AvailabilityChanged += AvailabilityChanged;

            this.trunkManager = trunkManager;
            this.trunkManager.TrunkUpdated += TrunkUpdated;

            this.serviceAddress = configuration.WcfServiceAddress;
            //this.domainName = configuration.DomainName;
            this.validator = new CustomUserNamePasswordValidator(@"administrator", configuration.AdministratorPassword);
        }

        public void Dispose()
        {
            try
            {
                enhancedPresence.AvailabilityChanged -= AvailabilityChanged;
                trunkManager.TrunkUpdated -= TrunkUpdated;

                if (serviceHost != null)
                {
                    serviceHost.Close();
                    serviceHost = null;
                }

                userz.Reset -= IUsers_Reloaded;
                userz.Added -= IUsers_Added;
                userz.Updated -= IUsers_Updated;
                userz.Removed -= IUsers_Removed;
            }
            catch
            {
            }
        }

        public void Start()
        {
            try
            {
                serviceHost = new ServiceHost(this, new Uri(serviceAddress));
                serviceHost.Credentials.ServiceCertificate.Certificate = new X509Certificate2(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + @"\OfficeSIP.pfx", "");
                serviceHost.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
                serviceHost.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = validator;

                var binding = new NetTcpBinding();
                binding.Security.Mode = SecurityMode.Message;
                binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

                serviceHost.Description.Behaviors.Add(new ServiceMetadataBehavior());
                serviceHost.AddServiceEndpoint(typeof(IWcfService), binding, "");
                serviceHost.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexTcpBinding(), "mex");

                serviceHost.Open();
            }
            catch
            {
                serviceHost = null;
            }
        }

        public string AdministratorPassword
        {
            get { return validator.Password; }
            set { validator.Password = value; }
        }

        #region class CustomUserNamePasswordValidator {...}

        class CustomUserNamePasswordValidator : UserNamePasswordValidator
        {
            public CustomUserNamePasswordValidator(string name, string password)
            {
                Name = name;
                Password = password;
            }

            public string Name
            {
                get;
                set;
            }

            public string Password
            {
                get;
                set;
            }

            public override void Validate(string userName, string password)
            {
                if (userName.ToLower() == Name && password == Password)
                    return;

                throw new FaultException(new FaultReason(""), new FaultCode("AccessDenied"));
            }
        }

        #endregion

        private void UpdateCallback()
        {
            if (callback != null)
                try
                {
                    callback.NewClient();
                }
                catch
                {
                }

            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IWcfServiceCallback>();
            }
            catch
            {
            }
        }

        #region Users

        void IWcfService.AddUser(string usersId, WcfUser user, string password)
        {
            try
            {
                var users = GetUsers(usersId);
                if (users.IsReadOnly == false)
                    users.Add(accounts.DefaultAccountId, user.ToIUser(password));
            }
            catch (UsersException)
            {
            }
        }

        void IWcfService.UpdateUser(string usersId, WcfUser user)
        {
            var users = GetUsers(usersId);

            if (users.IsReadOnly == false)
            {
                var oldUser = users.GetByName(accounts.DefaultAccountId, user.Name);

                users.Update(accounts.DefaultAccountId, user.ToIUser((oldUser == null) ? string.Empty : oldUser.Password));
            }
        }

        void IWcfService.RemoveUser(string usersId, string name)
        {
            var users = GetUsers(usersId);
            if (users.IsReadOnly == false)
                users.Remove(accounts.DefaultAccountId, name);
        }

        void IWcfService.SetUserPassword(string usersId, string name, string password)
        {
            var users = GetUsers(usersId);

            if (users.IsReadOnly == false)
            {
                var user = users.GetByName(accounts.DefaultAccountId, name);

                if (user != null)
                {
                    user.Password = password;
                    users.Update(0, user);
                }
            }
        }

        int IWcfService.GetUsersCount(string id)
        {
            return GetUsers(id).GetCount(accounts.DefaultAccountId);
        }

        IList<WcfUser> IWcfService.GetUsers(string id, int startIndex, int count, out int overallCount)
        {
            WcfUser[] result = null;

            var users = GetUsers(id);

            var items = users.GetUsers(accounts.DefaultAccountId, startIndex, count);
            var account = accounts.GetDefaultAccount();
            if (items != null && account != null)
            {
                result = new WcfUser[items.Count];

                for (int i = 0; i < result.Length; i++)
                    result[i] = new WcfUser(items[i])
                    {
                        Availability = enhancedPresence.GetAvailability(@"sip:" + items[i].Name + @"@" + account.DomainName),
                    };
            }

            overallCount = users.GetCount(accounts.DefaultAccountId);

            return result;
        }

        private IUsers GetUsers(string id)
        {
            var result = userz.Get(id);

            if (result == null)
                throw new Exception("Invalid users Id specified in request");

            return result;
        }

        private void OnUsersReset(string usersId)
        {
            if (callback != null)
            {
                try
                {
                    callback.UsersReset(usersId);
                }
                catch
                {
                    callback = null;
                }
            }
        }

        private void OnUserAddedOrUpdated(string usersId, WcfUser wcfUser)
        {
            if (callback != null)
            {
                try
                {
                    callback.UserAddedOrUpdated(usersId, wcfUser);
                }
                catch
                {
                    callback = null;
                }
            }
        }

        private void OnUserRemoved(string usersId, string name)
        {
            if (callback != null)
            {
                try
                {
                    callback.UserRemoved(usersId, name);
                }
                catch
                {
                    callback = null;
                }
            }
        }

        private void AvailabilityChanged(string aor, int availability)
        {
            aor = aor.Split(new char[] { ':', '@' })[1];
            if (callback != null)
                try
                {
                    callback.AvailabilityChanged(aor, availability);
                }
                catch
                {
                    callback = null;
                }
        }

        #endregion

        #region IUsers events

        private void IUsers_Reloaded(int accountId, IUsers source)
        {
            if (accountId == accounts.DefaultAccountId)
                OnUsersReset(source.Id);
        }

        private void IUsers_Added(int accountId, IUsers source, IUser user)
        {
            if (accountId == accounts.DefaultAccountId)
                OnUserAddedOrUpdated(source.Id, new WcfUser(user));
        }

        private void IUsers_Updated(int accountId, IUsers source, IUser user)
        {
            if (accountId == accounts.DefaultAccountId)
                OnUserAddedOrUpdated(source.Id, new WcfUser(user));
        }

        private void IUsers_Removed(int accountId, IUsers source, IUser user)
        {
            if (accountId == accounts.DefaultAccountId)
                OnUserRemoved(source.Id, user.Name);
        }

        #endregion

        #region VoIP Providers

        IEnumerable<WcfVoipProvider> IWcfService.GetVoipProviders()
        {
            var configuration = SipServerConfigurationSection.GetSection();
            var list = new List<WcfVoipProvider>();
            foreach (var provider in configuration.VoipProviders)
                list.Add(new WcfVoipProvider(provider, trunkManager.GetTrunk(provider.Username, provider.ServerHostname)));
            return list;
        }

        void IWcfService.AddVoipProvider(WcfVoipProvider provider)
        {
            var configuration = SipServerConfigurationSection.GetSection();
            configuration.VoipProviders.Add(provider.ToVoipProviderConfigurationElement());
            configuration.Save();
        }

        void IWcfService.RemoveVoipProvider(string username, string hostname)
        {
            var configuration = SipServerConfigurationSection.GetSection();
            configuration.VoipProviders.Remove(username, hostname);
            configuration.Save();
        }

        public void TrunkUpdated(Sip.Server.Trunk trunk)
        {
            if (callback != null)
            {
                try
                {
                    callback.VoipProviderUpdated(new WcfVoipProvider(trunk));
                }
                catch
                {
                    callback = null;
                }
            }
        }

        #endregion

        #region Configuration

        WcfConfiguration IWcfService.GetConfigurations()
        {
            UpdateCallback();

            var configuration = SipServerConfigurationSection.GetSection();

            var sources = new WcfUsers[userz.Count];
            for (int i = 0; i < userz.Count; i++)
                sources[i] = new WcfUsers(userz[i]);

            return new WcfConfiguration()
            {
                DomainName = accounts.GetDefaultAccount().DomainName,
                IsAuthorizationEnabled = configuration.IsAuthorizationEnabled,
                IsActiveDirectoryUsersEnabled = configuration.IsActiveDirectoryEnabled,
                ActiveDirectoryUsersGroup = configuration.ActiveDirectoryGroup,
                IsTracingEnabled = configuration.IsTracingEnabled,
                TracingFileName = configuration.TracingPath,
                Users = sources,
            };
        }

        void IWcfService.SetConfigurations(WcfConfiguration configurations)
        {
            var configuration = SipServerConfigurationSection.GetSection();

            accounts.SetAccount(
                new Account(accounts.GetDefaultAccount()) { DomainName = configurations.DomainName, });

            configuration.IsAuthorizationEnabled = configurations.IsAuthorizationEnabled;
            configuration.IsActiveDirectoryEnabled = configurations.IsActiveDirectoryUsersEnabled;
            configuration.ActiveDirectoryGroup = configurations.ActiveDirectoryUsersGroup;
            configuration.IsTracingEnabled = configurations.IsTracingEnabled;

            configuration.Save();
        }

        void IWcfService.SetAdministratorPassword(string newPassword)
        {
            var configuration = SipServerConfigurationSection.GetSection();

            configuration.AdministratorPassword = newPassword;

            configuration.Save();
        }

        #endregion

        #region Ping, GetVersion

        void IWcfService.Ping()
        {
        }

        Version IWcfService.GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        #endregion

        #region TURN Confoguration

        WcfTurnConfiguration IWcfService.GetTurnConfigurations()
        {
            var configuration = SipServerConfigurationSection.GetSection();

            var result = new WcfTurnConfiguration()
            {
                Key1 = configuration.TurnServers.Key1,
                Key2 = configuration.TurnServers.Key2,
            };

            if (configuration.TurnServers.Count > 0)
            {
                foreach (TurnServerConfigurationElement item in configuration.TurnServers)
                {
                    result.FQDN = item.Fqdn;
                    result.TCPPort = item.TcpPort;
                    result.UDPPort = item.UdpPort;
                    break;
                }
            }

            return result;
        }

        void IWcfService.SetTurnConfigurations(WcfTurnConfiguration wcf)
        {
            var configuration = SipServerConfigurationSection.GetSection();

            configuration.TurnServers.Key1 = wcf.Key1;
            configuration.TurnServers.Key2 = wcf.Key2;

            configuration.TurnServers.Clear();
            configuration.TurnServers.Add(wcf.FQDN, wcf.TCPPort, wcf.UDPPort, true);
            configuration.TurnServers.Add(wcf.FQDN, wcf.TCPPort, wcf.UDPPort, false);

            ////m_MRAS.SetTURN( configurations.FQDN, configurations.TCPPort, configurations.UDPPort, configurations.Key1, configurations.Key2 );

            configuration.Save();
        }

        #endregion

        #region XML configuration

        string IWcfService.GetDefaultXmlConfiguration()
        {
            return SipServerConfigurationSection.GetSection().DefaultXml;
        }

        string IWcfService.GetXmlConfiguration()
        {
            var configuration = SipServerConfigurationSection.GetSection();

            return configuration.ReadXml();
        }

        string[] IWcfService.SetXmlConfiguration(string xml)
        {
            var configuration = SipServerConfigurationSection.GetSection();

            var errors = configuration.Validate(xml);

            if (errors.Count == 0)
                configuration.WriteXml(xml);

            return ConvertErrors(errors);
        }

        string[] IWcfService.ValidateXmlConfiguration(string xml)
        {
            var configuration = SipServerConfigurationSection.GetSection();

            return ConvertErrors(configuration.Validate(xml));
        }

        private static string[] ConvertErrors(IList<Exception> errors1)
        {
            var errors2 = new string[errors1.Count];
            for (int i = 0; i < errors1.Count; i++)
                errors2[i] = errors1[i].Message;
            return errors2;
        }

        #endregion
    }
}

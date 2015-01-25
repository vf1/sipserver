using System;
using System.Security.Cryptography;
//using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ServiceProcess;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Text;
using System.Net;
using ControlPanel.Service1;
using DataVirtualization;

namespace ControlPanel
{
	// http://msdn.microsoft.com/en-us/library/ms731184.aspx
	// http://dotnetaddict.dotnetdevelopersjournal.com/wcf_alarmclock.htm

	public partial class Programme
		: Application
		, INotifyPropertyChanged
		, Service1.IWcfServiceCallback
	{
		public CommandBindingCollection CommandBindings { get; private set; }

		private Service1.WcfServiceClient WcfClient1 { get; set; }
		private ServiceController serviceController;
		private DispatcherTimer serviceTimer;
		private ServiceControllerStatus serviceStatus;
		private DispatcherTimer pingTimer;
		private const string minCompatibleVersion = "3.3";
		private const string serviceName = @"OfficeSIP Server";
		private const int baseUsersTabIndex = 1;

		public ConfigData Config { get; private set; }
		public bool UsersUpdating { get; private set; }
		public ObservableCollection<Service1.VoipProvider> VoipProviders { get; private set; }
		public bool VoipProvidersUpdating { get; private set; }
		public Version Version { get; private set; }
		public string CompatibleVersions { get; private set; }
		public Version ServerVersion { get; private set; }

		private Dictionary<string, AsyncVirtualizingCollection<User>> userz;

		private bool xmlConfigLoading;
		private string[] xmlConfigErrors;

		private void Application_Startup(object sender, StartupEventArgs e)
		{
#if DEBUG
			//InitializeCrashHandler();
#else
			InitializeCrashHandler();
#endif

			Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			string maxCompatibleVersions = Version.Major.ToString() + "." + Version.Minor.ToString();
			if (minCompatibleVersion != maxCompatibleVersions)
				CompatibleVersions = minCompatibleVersion + " - " + maxCompatibleVersions;
			else
				CompatibleVersions = minCompatibleVersion;

			CommandBindings = new CommandBindingCollection();
			CommandBindings.Add(new CommandBinding(Commands.Connect, ConnectBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.ConnectAs, ConnectAsBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.Close, CloseBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.CopyAll, CopyBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.ApplySettings, ApplySettingsBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.ResetSettings, ResetSettingsBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.AddUser, AddUserBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.RemoveUser, RemoveUserBinding_Executed, RemoveUserBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.EditUser, EditUserBinding_Executed, RemoveUserBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.ChangeUserPassword, ChangeUserPasswordBinding_Executed, RemoveUserBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.ChangeAdminPassword, ChangeAdminPasswordBinding_Executed, ChangeAdminPasswordBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.ServiceControl, ServiceControlBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.OpenService, OpenServiceBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.StartService, StartServiceBinding_Executed, StartServiceBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.StopService, StopServiceBinding_Executed, StopServiceBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.ShellExecute, ShellExecuteBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.ConfigTurnServer, ConfigTurnServerBinding_Executed));
			CommandBindings.Add(new CommandBinding(Commands.LoadXml, LoadXmlBinding_Executed, LoadXmlBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.LoadDefaultXml, LoadDefaultXmlBinding_Executed, LoadXmlBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.ValidateXml, ValidateXmlBinding_Executed, ValidateXmlBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.SaveXml, SaveXmlBinding_Executed, SaveXmlBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.AddVoipProvider, AddVoipProviderBinding_Executed, AddVoipProviderBinding_CanExecute));
			CommandBindings.Add(new CommandBinding(Commands.RemoveVoipProvider, RemoveVoipProviderBinding_Executed, RemoveVoipProviderBinding_CanExecute));

			serviceTimer = new DispatcherTimer();
			serviceTimer.Tick += ServiceTimer_Tick;
			serviceTimer.Interval = new TimeSpan(0, 0, 5);
			serviceTimer.IsEnabled = true;

			pingTimer = new DispatcherTimer();
			pingTimer.Tick += PingTimer_Tick;
			pingTimer.Interval = new TimeSpan(0, 0, 20);
			pingTimer.IsEnabled = true;

			CreateMainWindow<Login>(true);
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			if (serviceController != null)
				serviceController.Dispose();

			DestroyWcfService1Client();
		}

		public static Programme Instance
		{
			get { return Programme.Current as Programme; }
		}

		#region MainWindow [ CreateMainWindow, CloseMainWindow ]

		private T CreateMainWindow<T>(bool show)
			where T : Window, new()
		{
			CloseMainWindow();

			IsConnectingMode = false;

			T window = new T();

			MainWindow = window;

			if (typeof(T) == typeof(Login))
				MainWindow.DataContext = this;
			if (typeof(T) == typeof(ServiceOpen))
				MainWindow.DataContext = this;
			if (typeof(T) == typeof(ServiceControl))
				MainWindow.DataContext = this;
			if (typeof(T) == typeof(Main))
				MainWindow.DataContext = this;
			if (typeof(T) == typeof(Error))
				MainWindow.DataContext = this;
			if (typeof(T) == typeof(EditXmlConfig))
				MainWindow.DataContext = this;


			MainWindow.Closed += MainWindow_Closed;
			MainWindow.CommandBindings.AddRange(CommandBindings);

			if (show)
				MainWindow.Show();

			return window;
		}

		private void CloseMainWindow()
		{
			if (MainWindow != null)
			{
				MainWindow.Closed -= MainWindow_Closed;
				MainWindow.Close();
				MainWindow = null;
			}
		}

		#endregion

		#region IsConnectingMode GUI

		private bool isConnectingMode = false;

		public bool IsEditingEnabled
		{
			get
			{
				return !isConnectingMode;
			}
		}

		public Visibility ConnectingVisibility
		{
			get
			{
				return isConnectingMode ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public bool IsConnectingMode
		{
			set
			{
				if (isConnectingMode != value)
				{
					isConnectingMode = value;
					OnPropertyChanged(@"IsEditingEnabled");
					OnPropertyChanged(@"ConnectingVisibility");
				}
			}
		}

		#endregion

		#region ErrorMessage GUI

		private void CreateMainErrorWindow(Exception exception)
		{
			this.Exception = exception;
			CreateMainWindow<Error>(true);
		}

		private void CreateMainErrorWindow(string errorMessage)
		{
			ErrorMessage = errorMessage;
			CreateMainWindow<Error>(true);
		}

		public string errorMessage;

		public string ErrorMessage
		{
			get
			{
				return errorMessage;
			}
			set
			{
				errorMessage = value;
				OnPropertyChanged(@"ErrorMessage");
			}
		}

		public Exception Exception
		{
			set
			{
				string errorMessage = "";

				Exception exception = value;
				while (exception != null)
				{
					if (exception.Message.Length == 0 && exception is FaultException)
					{
						FaultException faultException = exception as FaultException;

						switch (faultException.Code.Name)
						{
							case @"AccessDenied":
								errorMessage += "Invalid username and/or password.";
								break;
							case @"AddAORError":
								errorMessage += "Error occurs when add user.";
								break;
							case @"RemoveAORError":
								errorMessage += "Error occurs when remove user.";
								break;
							case @"PasswordSetError":
								errorMessage += "Error occurs when change user's password.";
								break;
						}

						errorMessage += "\r\n\r\n";
					}
					else
					{
						errorMessage += exception.Message + "\r\n\r\n";
					}

					exception = exception.InnerException;
				}

				this.ErrorMessage = errorMessage;
			}
		}

		#endregion

		#region WcfService [ CreateWcfService1Client, DestroyWcfService1Client ]

		private void CreateWcfService1Client(string url, string user, string password)
		{
			NetTcpBinding tcpBinding = new NetTcpBinding();
			tcpBinding.Security.Mode = SecurityMode.Message;
			tcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

			EndpointAddress remoteAddress = new EndpointAddress(
				new Uri(url),
				EndpointIdentity.CreateDnsIdentity(serviceName),
				new AddressHeader[] { AddressHeader.CreateAddressHeader("adh1", url, 1) }
				);

			WcfClient1 = new Service1.WcfServiceClient(new InstanceContext(this), tcpBinding, remoteAddress);

			WcfClient1.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode =
				System.ServiceModel.Security.X509CertificateValidationMode.None;
			WcfClient1.ClientCredentials.UserName.UserName = user;
			WcfClient1.ClientCredentials.UserName.Password = GetPasswordHash(password);

			WcfClient1.GetConfigurationsCompleted += WcfClient1_GetConfigurationsCompleted;
			WcfClient1.SetConfigurationsCompleted += WcfClient1_SetConfigurationsCompleted;
			WcfClient1.AddUserCompleted += WcfClient1_AddUserCompleted;
			WcfClient1.RemoveUserCompleted += WcfClient1_RemoveUserCompleted;
			WcfClient1.UpdateUserCompleted += WcfClient1_UpdateUserCompleted;
			WcfClient1.SetUserPasswordCompleted += WcfClient1_SetUserPasswordCompleted;
			WcfClient1.PingCompleted += WcfClient1_PingCompleted;
			WcfClient1.GetTurnConfigurationsCompleted += WcfClient1_GetTurnConfigurationsCompleted;
			WcfClient1.GetDefaultXmlConfigurationCompleted += WcfClient1_GetDefaultXmlConfigurationCompleted;
			WcfClient1.GetXmlConfigurationCompleted += WcfClient1_GetXmlConfigurationCompleted;
			WcfClient1.ValidateXmlConfigurationCompleted += WcfClient1_ValidateXmlConfigurationCompleted;
			WcfClient1.SetXmlConfigurationCompleted += WcfClient1_SetXmlConfigurationCompleted;
			WcfClient1.GetVoipProvidersCompleted += WcfClient1_GetVoipProvidersCompleted;
			WcfClient1.AddVoipProviderCompleted += WcfClient1_AddVoipProviderCompleted;
			WcfClient1.RemoveVoipProviderCompleted += WcfClient1_RemoveVoipProviderCompleted;
			WcfClient1.SetAdministratorPasswordCompleted += WcfClient1_SetAdministratorPasswordCompleted;
			WcfClient1.GetVersionCompleted += WcfClient1_GetVersionCompleted;
		}

		private void DestroyWcfService1Client()
		{
			if (WcfClient1 != null)
			{
				//WcfClient1.GetConfigurationsCompleted -= new EventHandler<Service1.GetConfigurationsCompletedEventArgs>(WcfClient1_GetConfigurationsCompleted);
				//WcfClient1.SetConfigurationsCompleted -= new EventHandler<AsyncCompletedEventArgs>(WcfClient1_SetConfigurationsCompleted);
				////WcfClient1.GetAORsCompleted -= new EventHandler<ControlPanel.Service1.GetAORsCompletedEventArgs>(WcfClient1_GetAORsCompleted);
				//WcfClient1.AddUserCompleted -= new EventHandler<AsyncCompletedEventArgs>(WcfClient1_AddUserCompleted);
				////WcfClient1.RemoveAORCompleted -= new EventHandler<AsyncCompletedEventArgs>(WcfClient1_RemoveAORCompleted);
				////WcfClient1.SetAORPasswordCompleted -= new EventHandler<AsyncCompletedEventArgs>(WcfClient1_SetAORPasswordCompleted);
				////WcfClient1.UpdateAORCompleted -= new EventHandler<AsyncCompletedEventArgs>(WcfClient1_UpdateAORCompleted);
				//WcfClient1.PingCompleted -= new EventHandler<AsyncCompletedEventArgs>(WcfClient1_PingCompleted);
				//WcfClient1.GetTurnConfigurationsCompleted -= new EventHandler<ControlPanel.Service1.GetTurnConfigurationsCompletedEventArgs>(WcfClient1_GetTURNConfigurationsCompleted);
				//WcfClient1.GetXmlConfigurationCompleted -= new EventHandler<ControlPanel.Service1.GetXmlConfigurationCompletedEventArgs>(WcfClient1_GetXmlConfigurationCompleted);
				//WcfClient1.ValidateXmlConfigurationCompleted -= new EventHandler<ControlPanel.Service1.ValidateXmlConfigurationCompletedEventArgs>(WcfClient1_ValidateXmlConfigurationCompleted);
				//WcfClient1.SetXmlConfigurationCompleted -= new EventHandler<ControlPanel.Service1.SetXmlConfigurationCompletedEventArgs>(WcfClient1_SetXmlConfigurationCompleted);
				//WcfClient1.GetVoipProvidersCompleted -= new EventHandler<ControlPanel.Service1.GetVoipProvidersCompletedEventArgs>(WcfClient1_GetVoipProvidersCompleted);

				if (WcfClient1.State == CommunicationState.Opened)
					WcfClient1.Close();

				WcfClient1 = null;
			}
		}

		private string GetPasswordHash(string password)
		{
			using (var md5 = MD5.Create())
			{
				var b1 = Encoding.UTF8.GetBytes(password + @"{ED3F2734-AFBA-4f5f-AD40-5F01917C5926}");
				md5.TransformFinalBlock(b1, 0, b1.Length);

				return BitConverter.ToString(md5.Hash).Replace(@"-", "").ToLower();
			}
		}

		#endregion

		#region ShellExecute

		private void ShellExecuteBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			using (System.Diagnostics.Process process = new System.Diagnostics.Process())
			{
				process.StartInfo.FileName = e.Parameter as string;
				process.StartInfo.UseShellExecute = true;
				process.Start();
			}
		}

		#endregion

		#region Edit XML Configuartion

		public string XmlConfig { get; set; }

		public string[] XmlConfigErrors
		{
			get { return xmlConfigErrors; }
			set
			{
				if (xmlConfigErrors != value)
				{
					xmlConfigErrors = value;
					OnPropertyChanged(@"XmlConfigErrors");
				}
			}
		}

		public bool XmlConfigLoading
		{
			get { return xmlConfigLoading; }
			set
			{
				if (xmlConfigLoading != value)
				{
					xmlConfigLoading = value;
					OnPropertyChanged(@"XmlConfigLoading");
					OnPropertyChanged(@"XmlConfigOperationFinished");
				}
			}
		}

		public bool XmlConfigOperationFinished
		{
			get { return !xmlConfigLoading; }
		}

		private void LoadDefaultXmlBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			XmlConfigLoading = true;
			WcfClient1.GetDefaultXmlConfigurationAsync();
		}

		private void WcfClient1_GetDefaultXmlConfigurationCompleted(object sender, GetDefaultXmlConfigurationCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
				UpdateXml(e.Result);

			XmlConfigLoading = false;
		}

		private void LoadXmlBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			XmlConfigLoading = true;
			WcfClient1.GetXmlConfigurationAsync();
		}

		private void WcfClient1_GetXmlConfigurationCompleted(object sender, GetXmlConfigurationCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
				UpdateXml(e.Result);

			XmlConfigLoading = false;
		}

		private void UpdateXml(string result)
		{
			if (this.MainWindow is EditXmlConfig == false)
				CreateMainWindow<EditXmlConfig>(true);

			XmlConfig = result;
			OnPropertyChanged(@"XmlConfig");
		}

		private void LoadXmlBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = XmlConfigOperationFinished;
		}

		private void ValidateXmlBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			WcfClient1.ValidateXmlConfigurationAsync(XmlConfig);
		}

		private void ValidateXmlBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !string.IsNullOrEmpty(XmlConfig) && XmlConfigOperationFinished;
		}

		private void WcfClient1_ValidateXmlConfigurationCompleted(object sender, ControlPanel.Service1.ValidateXmlConfigurationCompletedEventArgs e)
		{
			if (ValidateState(e))
				XmlConfigErrors = e.Result;
		}

		private void SaveXmlBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			WcfClient1.SetXmlConfigurationAsync(XmlConfig);
		}

		private void SaveXmlBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !string.IsNullOrEmpty(XmlConfig) && XmlConfigOperationFinished;
		}

		private void WcfClient1_SetXmlConfigurationCompleted(object sender, ControlPanel.Service1.SetXmlConfigurationCompletedEventArgs e)
		{
			if (ValidateState(e))
				XmlConfigErrors = e.Result;
		}

		private bool ValidateState(AsyncCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				XmlConfigErrors = new string[] { "Unable connect to the server: " + e.Error.Message };
				return false;
			}

			return true;
		}

		#endregion

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			this.Shutdown();
		}

		private void ConnectAsBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			CreateMainWindow<Login>(true);
		}

		private void ConnectBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				if (MainWindow is Login == false)
					CreateMainWindow<Login>(true);
				IsConnectingMode = true;

				Login login = MainWindow as Login;
				CreateWcfService1Client(login.url.Text, login.user.Text, login.password.Password);
				if (login.classicMode.IsChecked == true)
					WcfClient1.GetConfigurationsAsync();
				else
					WcfClient1.GetXmlConfigurationAsync();
				WcfClient1.GetVersionAsync();
			}
			catch (Exception ex)
			{
				CreateMainErrorWindow(ex);
			}
		}

		private void ServiceControlBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (VistaSecurity.IsAdmin())
			{
				CreateMainWindow<ServiceOpen>(true);
			}
			else
			{
				VistaSecurity.RestartElevated();
			}
		}

		private void OpenServiceBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				if (MainWindow is ServiceOpen)
				{
					IsConnectingMode = true;

					ServiceOpen serviceOpen = MainWindow as ServiceOpen;

					if (serviceController != null)
						serviceController.Dispose();
					if (serviceOpen.machineName.Text == @"localhost")
						serviceController = new ServiceController(serviceName);
					else
						serviceController = new ServiceController(serviceName, serviceOpen.machineName.Text);

					CreateMainWindow<ServiceControl>(true);
				}
				else
					throw new NotSupportedException();

				var dummy = serviceController.Status;
			}
			catch (Exception ex)
			{
				CreateMainErrorWindow(ex);
			}
		}

		public string ServiceStatus
		{
			get
			{
				switch (serviceController.Status)
				{
					case ServiceControllerStatus.ContinuePending:
						return @"Continue pending...";
					case ServiceControllerStatus.Paused:
						return @"Paused";
					case ServiceControllerStatus.PausePending:
						return @"Pause pending...";
					case ServiceControllerStatus.Running:
						return @"Running";
					case ServiceControllerStatus.StartPending:
						return @"Start pending...";
					case ServiceControllerStatus.Stopped:
						return @"Stopped";
					case ServiceControllerStatus.StopPending:
						return @"Stop pending...";
					default:
						return @"Unknown";
				}
			}
		}

		private void ServiceTimer_Tick(object sender, EventArgs e)
		{
			if (serviceController != null && this.MainWindow is ServiceControl)
			{
				try
				{
					serviceController.Refresh();
					if (this.serviceStatus != serviceController.Status)
					{
						this.serviceStatus = serviceController.Status;
						this.OnPropertyChanged(@"ServiceStatus");

						CommandManager.InvalidateRequerySuggested();
					}
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

		private void StartServiceBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				serviceController.Start();
				ServiceTimer_Tick(this, null);
			}
			catch
			{
				MessageBox.Show(@"Failed to start service. Possible you have no enough rights.");
			}
		}

		private void StartServiceBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			try
			{
				e.CanExecute =
					(serviceController != null && serviceController.Status == ServiceControllerStatus.Stopped);
			}
			catch
			{
				e.CanExecute = false;
			}
			e.Handled = true;
		}

		private void StopServiceBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				serviceController.Stop();
				ServiceTimer_Tick(this, null);
			}
			catch
			{
				MessageBox.Show(@"Failed to stop service. Possible you have no enough rights.");
			}
		}

		private void StopServiceBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			try
			{
				e.CanExecute =
					(serviceController != null && serviceController.Status == ServiceControllerStatus.Running
					&& serviceController.CanStop);
			}
			catch
			{
				e.CanExecute = false;
			}
			e.Handled = true;
		}

		private void CloseBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			this.Shutdown();
		}

		private void CopyBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Source is TextBox)
			{
				(e.Source as TextBox).SelectAll();
				(e.Source as TextBox).Copy();
			}
		}

		private void AddUserBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			AddUser addUser = new AddUser();
			addUser.Owner = this.MainWindow;
			addUser.DataContext = this;

			if (addUser.ShowDialog() == true)
			{
				string id = (e.Source as UsersTabContent).Id;

				var user = new Service1.User()
				{
					Name = addUser.username.Text,
					DisplayName = addUser.displayName.Text,
					Email = addUser.email.Text,
				};

				WcfClient1.AddUserAsync(id, user, addUser.password.Password, id);
			}
		}

		void WcfClient1_AddUserCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
			}
		}

		private void RemoveUserBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var content = e.Source as UsersTabContent;

			foreach (DataWrapper<User> user in content.UsersList.SelectedItems)
				if (user.IsLoading == false)
					WcfClient1.RemoveUser(content.Id, user.Data.Name);
		}

		private void RemoveUserBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var listView = (e.Source as UsersTabContent).UsersList;

			if (listView == null)
				listView = (e.Source as UsersTabContent).UsersList;

			e.CanExecute = (listView.SelectedIndex >= 0);
		}

		private void WcfClient1_RemoveUserCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
			}
		}

		private void EditUserBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var content = e.Source as UsersTabContent;
			var user = (content.UsersList.SelectedItem as DataWrapper<User>).Data;

			EditUser editUser = new EditUser();
			editUser.Owner = this.MainWindow;
			editUser.DataContext = this;
			editUser.Username.Text = user.Name;
			editUser.displayName.Text = user.DisplayName;
			editUser.email.Text = user.Email;
			if (editUser.ShowDialog() == true)
			{
				user.DisplayName = editUser.displayName.Text;
				user.Email = editUser.email.Text;

				WcfClient1.UpdateUserAsync(content.Id, user);
			}
		}

		private void WcfClient1_UpdateUserCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
			}
		}

		private void ChangeUserPasswordBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var content = e.Source as UsersTabContent;
			var user = (content.UsersList.SelectedItem as DataWrapper<User>).Data;

			NewPassword newPassword = new NewPassword();
			newPassword.Owner = this.MainWindow;
			newPassword.DataContext = this;
			newPassword.Username.Text = user.Name;
			if (newPassword.ShowDialog() == true)
				WcfClient1.SetUserPasswordAsync(content.Id, user.Name, newPassword.Password.Password);
		}

		private void WcfClient1_SetUserPasswordCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
				//if (e.UserState as string == @"admin")
				//{
				//    AdminPassChanging = false;
				//    OnPropertyChanged(@"AdminPassChanging");
				//}
			}
		}

		private void ChangeAdminPasswordBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ChangeAdminPasswordBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			NewPassword newPassword = new NewPassword();
			newPassword.Owner = this.MainWindow;
			newPassword.DataContext = this;
			newPassword.Username.Text = "administrator";
			if (newPassword.ShowDialog() == true)
				WcfClient1.SetAdministratorPasswordAsync(GetPasswordHash(newPassword.Password.Password));
		}

		private void WcfClient1_SetAdministratorPasswordCompleted(object sender, AsyncCompletedEventArgs e)
		{
			ValidateAsyncErrors(e);
		}

		private bool ValidateAsyncErrors(System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				CreateMainErrorWindow(e.Error);

				return false;
			}

			return true;
		}

		private void WcfClient1_GetVersionCompleted(object sender, GetVersionCompletedEventArgs e)
		{
			if (e.Error == null)
			{
				ServerVersion = e.Result;
				OnPropertyChanged(@"ServerVersion");

				CheckForNewVersion();
			}
		}

		#region Check for new version

		private Uri updateUri = new Uri("http://www.officesip.com/update.php");

		private void CheckForNewVersion()
		{
			var webClient = new WebClient();
			webClient.QueryString.Add("app", "SRV");
			webClient.QueryString.Add("ver", ServerVersion.ToString());
			webClient.DownloadStringCompleted += webClient_DownloadStringCompleted;
			webClient.DownloadStringAsync(updateUri);
		}

		struct UpdateResponse
		{
			public string Version;
			public DateTime Date;
			public string Url;

			public Version VersionEx
			{
				get { return string.IsNullOrEmpty(Version) ? new Version() : new Version(Version); }
			}
		}

		private void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			try
			{
				if (e.Error == null)
				{
					var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
					var response = serializer.Deserialize<UpdateResponse>(e.Result);

					if (ServerVersion != null && ServerVersion.CompareTo(response.VersionEx) < 0)
					{
						if (MainWindow is Main || MainWindow is EditXmlConfig)
						{
							var window = new NewVersion();
							window.Owner = MainWindow;
							window.Url = response.Url;
							window.Version = response.Version;
							window.CommandBindings.AddRange(CommandBindings);
							window.Show();
						}
					}
				}
			}
			catch
			{
			}
		}

		#endregion

		#region Get/Set Configuration

		private void ResetSettingsBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			//Config.BeginUpdating();
			//WcfClient1.GetConfigurationsAsync();
		}

		private AsyncVirtualizingCollection<User> GetUsers(string id)
		{
			if (userz.ContainsKey(id) == false)
				userz[id] = new AsyncVirtualizingCollection<User>(new UsersFetcher(WcfClient1, id), 32, 20); ;

			return userz[id];
		}

		private void ReloadUsers(string id)
		{
			if (userz.ContainsKey(id))
			{
				var users = userz[id];
				users.Reset();
			}
		}

		private User FindUserByName(string id, string name)
		{
			if (userz.ContainsKey(id))
			{
				var users = userz[id];
				foreach (var user in users)
					if (string.Compare(user.Data.Name, name, true) == 0)
						return user.Data;
			}

			return null;
		}

		private void WcfClient1_GetConfigurationsCompleted(object sender, Service1.GetConfigurationsCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
				if (this.MainWindow is Main == false)
				{
					//					AdminPassChanging = false;
					UsersUpdating = true;
					userz = new Dictionary<string, AsyncVirtualizingCollection<User>>();
					//new List<AsyncVirtualizingCollection<User>>();
					VoipProvidersUpdating = true;
					VoipProviders = new ObservableCollection<Service1.VoipProvider>();
					Config = new ConfigData();

					var main = CreateMainWindow<Main>(true);

					if (e.Result.Users != null)
					{
						int index = baseUsersTabIndex;
						foreach (var usersDesc in e.Result.Users)
						{
							var content = new UsersTabContent(usersDesc) { DataContext = this, };
							content.UsersList.ItemsSource = GetUsers(usersDesc.Id);
							if (usersDesc.IsReadOnly)
								content.EditControls.Visibility = Visibility.Collapsed;

							main.tabControl1.Items.Insert(index++, new TabItem()
							{
								Header = new UsersTabHeader(usersDesc.SourceName),
								Content = content,
							});
						}
					}
				}

				Config.SetConfing(e.Result);
				Config.EndUpdating();

				WcfClient1.GetVoipProvidersAsync();
				WcfClient1.GetTurnConfigurationsAsync();
			}
		}

		public TURNConfigurations TurnConfig
		{
			get;
			private set;
		}

		private void WcfClient1_GetTurnConfigurationsCompleted(object sender, GetTurnConfigurationsCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
				TurnConfig = e.Result;
				OnPropertyChanged(@"TurnConfig");
			}
		}

		private void ApplySettingsBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Config.BeginUpdating();
			WcfClient1.SetConfigurationsAsync(
				Config.GetConfing()
				);
		}

		private void WcfClient1_SetConfigurationsCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
				Config.EndUpdating();
				Config.UpdateRebootRequred();
			}
		}

		#endregion

		//void WcfClient1_GetAORsCompleted(object sender, ControlPanel.Service1.GetAORsCompletedEventArgs e)
		//{
		//    if (ValidateAsyncErrors(e))
		//    {
		//        Users.Clear();
		//        foreach (Service1.AOR aor in e.Result)
		//            Users.Add(new UserData(aor));
		//        UsersUpdating = false;
		//        OnPropertyChanged(@"UsersUpdating");
		//    }
		//}

		void WcfClient1_GetVoipProvidersCompleted(object sender, GetVoipProvidersCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
				VoipProviders.Clear();
				foreach (var provider in e.Result)
					VoipProviders.Add(provider);
				VoipProvidersUpdating = false;
				OnPropertyChanged(@"VoipProvidersUpdating");
			}
		}

		void PingTimer_Tick(object sender, EventArgs e)
		{
			if (this.MainWindow is Main && this.WcfClient1 != null)
				this.WcfClient1.PingAsync();
		}

		void WcfClient1_PingCompleted(object sender, AsyncCompletedEventArgs e)
		{
			ValidateAsyncErrors(e);
		}

		#region IWCFServiceCallback

		public void AvailabilityChanged(string name, int availability)
		{
			ForEachFetchedUser(name,
				(wrapper) =>
				{
					var data = wrapper.Data;
					if (data != null)
						data.Availability = availability;
				});
		}

		public void NewClient()
		{
			CreateMainErrorWindow(@"This Control Panel was kicked out from the server, because of new one connected to the server.");
		}

		public void UserAddedOrUpdated(string usersId, User user)
		{
			bool found = false;

			ForEachFetchedUser(user.Name,
				(wrapper) =>
				{
					var data = wrapper.Data;
					if (data != null)
					{
						data.DisplayName = user.DisplayName;
						data.Email = user.Email;

						found = true;
					}
				});

			if (found == false)
				ReloadUsers(usersId);
		}

		public void UserRemoved(string usersId, string name)
		{
			ReloadUsers(usersId);
		}

		public void UsersReset(string usersId)
		{
			ReloadUsers(usersId);
		}

		private static readonly Func<DataWrapper<User>, string, bool> compareName =
			(wrapper, name) =>
			{
				return wrapper.Data != null && wrapper.Data.Name == name;
			};

		private void ForEachFetchedUser(string name, Action<DataWrapper<User>> action)
		{
			foreach (var pair in userz)
			{
				var users = pair.Value;

				if (users != null)
				{
					var user = users.FindFetched(compareName, name);
					if (user != null)
						action(user);
				}
			}
		}

		#region UNUSED ASYNC CALLBACKS

		public IAsyncResult BeginUsersReset(string usersId, AsyncCallback ac, object o)
		{
			return null;
		}

		public IAsyncResult BeginUserAddedOrUpdated(string usersId, Service1.User user, AsyncCallback ac, object o)
		{
			return null;
		}

		public IAsyncResult BeginUserRemoved(string usersId, string name, AsyncCallback ac, object o)
		{
			return null;
		}

		public IAsyncResult BeginVoipProviderUpdated(Service1.VoipProvider provider, AsyncCallback ac, object o)
		{
			return null;
		}

		public void EndUsersReset(IAsyncResult ar)
		{
		}

		public void EndUserAddedOrUpdated(IAsyncResult ar)
		{
		}

		public void EndUserRemoved(IAsyncResult ar)
		{
		}

		public void EndVoipProviderUpdated(IAsyncResult ar)
		{
		}

		public IAsyncResult BeginAvailabilityChanged(string aor, int availability, AsyncCallback ac, object o)
		{
			return null;
		}

		public void EndAvailabilityChanged(IAsyncResult ar)
		{
		}

		public IAsyncResult BeginNewClient(AsyncCallback ac, object o)
		{
			return null;
		}

		public void EndNewClient(IAsyncResult ar)
		{
		}

		#endregion

		#endregion

		#region TurnWcfService [ CreateTurnWcfService1Client, DestroyTurnWcfService1Client ]

		//private void CreateTurnWcfService1Client(string url, string user, string password)
		//{
		//    NetTcpBinding tcpBinding = new NetTcpBinding();
		//    tcpBinding.Security.Mode = SecurityMode.Message;
		//    tcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

		//    EndpointAddress remoteAddress = new EndpointAddress(
		//        new Uri(url),
		//        EndpointIdentity.CreateDnsIdentity("OfficeSIP Turn Server"),
		//        new AddressHeader[] { AddressHeader.CreateAddressHeader("adh1", url, 1) }
		//        );

		//    turnWcfClient1 = new TurnService1.WcfTurnServiceClient(tcpBinding, remoteAddress);

		//    turnWcfClient1.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode =
		//        System.ServiceModel.Security.X509CertificateValidationMode.None;
		//    turnWcfClient1.ClientCredentials.UserName.UserName = user;
		//    turnWcfClient1.ClientCredentials.UserName.Password = password;

		//    turnWcfClient1.GetConfigurationCompleted += TurnWcfClient1_GetConfigurationCompleted;
		//}

		//private void DestroyTurnWcfService1Client()
		//{
		//    if (turnWcfClient1 != null)
		//    {
		//        turnWcfClient1.GetConfigurationCompleted -= TurnWcfClient1_GetConfigurationCompleted;

		//        if (turnWcfClient1.State == CommunicationState.Opened)
		//            turnWcfClient1.Close();

		//        WcfClient1 = null;
		//    }
		//}

		#endregion

		#region TurnServerCommands

		private static TurnService1.WcfTurnServiceClient CreateTurnWcfService1Client(string url, string username, string password)
		{
			NetTcpBinding tcpBinding = new NetTcpBinding();
			tcpBinding.Security.Mode = SecurityMode.Message;
			tcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

			EndpointAddress remoteAddress = new EndpointAddress(
				new Uri(url),
				EndpointIdentity.CreateDnsIdentity("OfficeSIP Turn Server"),
				new AddressHeader[] { AddressHeader.CreateAddressHeader("adh1", url, 1) }
				);

			var turnWcfClient1 = new TurnService1.WcfTurnServiceClient(tcpBinding, remoteAddress);

			turnWcfClient1.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode =
				System.ServiceModel.Security.X509CertificateValidationMode.None;
			turnWcfClient1.ClientCredentials.UserName.UserName = username;
			turnWcfClient1.ClientCredentials.UserName.Password = password;

			return turnWcfClient1;
		}

		private void ConfigTurnServerBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			LoginTurnServer login = new LoginTurnServer();
			login.address.Text = (TurnConfig != null) ? TurnConfig.FQDN : "";
			if (login.ShowDialog() == true)
			{
				try
				{
					if (string.IsNullOrEmpty(login.address.Text))
					{
						TurnConfig.FQDN = "";
						TurnConfig.UDPPort = 0;
						TurnConfig.TCPPort = 0;
						TurnConfig.Key1 = new byte[20];
						TurnConfig.Key2 = new byte[20];

						WcfClient1.SetTurnConfigurations(TurnConfig);
					}
					else
					{
						var serviceClient = CreateTurnWcfService1Client(login.url.Text, login.username.Text, login.password.Password);
						var configuration = serviceClient.GetConfiguration();

						EditTurnServer edit = new EditTurnServer();
						edit.adminName.Text = login.username.Text;
						edit.adminPass.Password = login.password.Password;
						edit.serverAddress.Text = login.address.Text;
						edit.turnUdpPort.Text = configuration.TurnUdpPort.ToString();
						edit.turnTcpPort.Text = configuration.TurnTcpPort.ToString();
						edit.turnTlsPort.Text = configuration.TurnTlsPort.ToString();
						if (configuration.TurnTcpPort == TurnConfig.TCPPort)
							edit.useTcp.IsChecked = true;
						else
							edit.useTls.IsChecked = true;
						if (configuration.PublicIp != null)
							edit.publicIp.Text = configuration.PublicIp.ToString();
						edit.publicMinPort.Text = configuration.MinPort.ToString();
						edit.publicMaxPort.Text = configuration.MaxPort.ToString();
						edit.realm.Text = configuration.Realm;
						if (configuration.Key1 != null)
							edit.key1.Text = Convert.ToBase64String(configuration.Key1);
						if (configuration.Key2 != null)
							edit.key2.Text = Convert.ToBase64String(configuration.Key2);

						if (edit.ShowDialog() == true)
						{
							configuration.AdminName = edit.adminName.Text;
							configuration.AdminPass = edit.adminPass.Password;
							configuration.TurnUdpPort = int.Parse(edit.turnUdpPort.Text);
							configuration.TurnTcpPort = int.Parse(edit.turnTcpPort.Text);
							configuration.TurnTlsPort = int.Parse(edit.turnTlsPort.Text);
							configuration.PublicIp = System.Net.IPAddress.Parse(edit.publicIp.Text);
							configuration.MinPort = int.Parse(edit.publicMinPort.Text);
							configuration.MaxPort = int.Parse(edit.publicMaxPort.Text);
							configuration.Realm = edit.realm.Text;
							configuration.Key1 = Convert.FromBase64String(edit.key1.Text);
							configuration.Key2 = Convert.FromBase64String(edit.key2.Text);

							serviceClient.SetConfiguration(configuration);

							TurnConfig.FQDN = edit.serverAddress.Text;
							TurnConfig.UDPPort = configuration.TurnUdpPort;
							TurnConfig.TCPPort = (edit.useTcp.IsChecked == true)
								? configuration.TurnTcpPort : configuration.TurnTlsPort;
							TurnConfig.Key1 = configuration.Key1;
							TurnConfig.Key2 = configuration.Key2;

							WcfClient1.SetTurnConfigurations(TurnConfig);
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}
			}
		}

		#endregion

		#region VoIP Providers Add/Remove

		public void VoipProviderUpdated(Service1.VoipProvider provider)
		{
			var oldProvider = FindVoipProvider(provider.Username, provider.Hostname);
			if (oldProvider == null)
				VoipProviders.Add(provider);
			else
			{
				if (provider.ErrorMessage != null)
					oldProvider.ErrorMessage = provider.ErrorMessage;
				if (provider.ForwardCallTo != null)
					oldProvider.ForwardCallTo = provider.ForwardCallTo;
				if (provider.DisplayName != null)
					oldProvider.DisplayName = provider.DisplayName;
				if (provider.AuthenticationId != null)
					oldProvider.AuthenticationId = provider.AuthenticationId;
				if (provider.LocalEndPoint != null)
					oldProvider.LocalEndPoint = provider.LocalEndPoint;
				if (provider.OutgoingProxy != null)
					oldProvider.OutgoingProxy = provider.OutgoingProxy;
				if (provider.Transport != null)
					oldProvider.Transport = provider.Transport;
			}
		}

		private void AddVoipProviderBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new AddVoipProvider();
			dialog.Owner = this.MainWindow;
			dialog.DataContext = this;
			if (dialog.ShowDialog() == true)
			{
				var provider = new Service1.VoipProvider()
				{
					Username = dialog.username.Text,
					Hostname = dialog.hotsname.Text,
					DisplayName = dialog.displayName.Text,
					AuthenticationId = dialog.authId.Text,
					OutgoingProxy = dialog.outgoingProxy.Text,
					LocalEndPoint = ConvertToIPEndPoint(dialog.localEndPoint.Text),
					Transport = dialog.transport.Text,
					ForwardCallTo = dialog.forwardCallTo.Text,
					Password = dialog.password.Password,
				};

				WcfClient1.AddVoipProviderAsync(provider, provider);
			}
		}

		private void AddVoipProviderBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
			//e.CanExecute = (VoipProviders.Count < 1);
		}


		void WcfClient1_AddVoipProviderCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
				var provider = e.UserState as Service1.VoipProvider;
				if (FindVoipProvider(provider.Username, provider.Hostname) == null)
					VoipProviders.Add(provider);
			}
		}

		private Service1.VoipProvider FindVoipProvider(string username, string hostname)
		{
			foreach (var provider in VoipProviders)
				if (provider.Username == username && provider.Hostname == hostname)
					return provider;
			return null;
		}

		private void RemoveVoipProviderBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ListView list = e.Source as ListView;
			foreach (Service1.VoipProvider item in list.SelectedItems)
				WcfClient1.RemoveVoipProviderAsync(item.Username, item.Hostname);
			for (int i = list.SelectedItems.Count - 1; i >= 0; i--)
				VoipProviders.Remove(list.SelectedItems[i] as Service1.VoipProvider);
		}

		void WcfClient1_RemoveVoipProviderCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if (ValidateAsyncErrors(e))
			{
			}
		}

		private void RemoveVoipProviderBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			ListView list = e.Source as ListView;
			e.CanExecute = (list.SelectedIndex >= 0);
		}

		#endregion

		#region Helpers

		private IPEndPoint ConvertToIPEndPoint(string value)
		{
			if (string.IsNullOrEmpty(value))
				return new IPEndPoint(IPAddress.None, 0);

			int port = 0;
			IPAddress address;

			int hcolon = value.IndexOf(':');
			if (hcolon >= 0)
			{
				if (int.TryParse(value.Substring(hcolon + 1), out port) == false || port > 65535 || port < 0)
					throw new ArgumentOutOfRangeException();

				value = value.Substring(0, hcolon);
			}

			if (IPAddress.TryParse(value, out address) == false)
				throw new ArgumentOutOfRangeException();

			return new IPEndPoint(address, port);
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(String property)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		private void OnPropertyChanged(PropertyChangedEventArgs eventArgs)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, eventArgs);
		}

		#endregion
	}
}

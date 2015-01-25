using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace ControlPanel
{
	public class ConfigData
		: INotifyPropertyChanged
	{
		Service1.Configurations oldConfig;

		public ConfigData()
		{
			RebootRequired = false;
			Updating = true;
		}

		public void SetConfing(Service1.Configurations config)
		{
			HomeDomain = config.DomainName;
			AuthDisabled = !config.IsAuthorizationEnabled;
			ActiveDirectoryMode = config.IsActiveDirectoryUsersEnabled;
			ActiveDirectoryGroup = config.ActiveDirectoryUsersGroup;
			EnableTracing = config.IsTracingEnabled;
			TracingPath = config.TracingFileName;

			oldConfig = config;
		}

		public Service1.Configurations GetConfing()
		{
			Service1.Configurations config = new Service1.Configurations();

			config.DomainName = HomeDomain;
			config.IsAuthorizationEnabled = !AuthDisabled;
			config.IsActiveDirectoryUsersEnabled = ActiveDirectoryMode;
			config.ActiveDirectoryUsersGroup = ActiveDirectoryGroup;
			config.IsTracingEnabled = EnableTracing;
			config.TracingFileName = TracingPath;

			return config;
		}

		public void UpdateRebootRequred()
		{
			if (HomeDomain != oldConfig.DomainName ||
				ActiveDirectoryMode != oldConfig.IsActiveDirectoryUsersEnabled)
			{
				RebootRequired = true;
				OnPropertyChanged(@"RebootRequired");
			}
		}

		public void BeginUpdating()
		{
			if (Updating != true)
			{
				Updating = true;
				OnPropertyChanged(@"Updating");
			}
		}

		public void EndUpdating()
		{
			if (Updating != false)
			{
				Updating = false;
				NotifyPropertiesChanged();
			}
		}

		public bool Updating { get; private set; }
		public string HomeDomain { get; set; }
		public bool AuthDisabled { get; set; }
		public bool ActiveDirectoryMode { get; set; }
		public string ActiveDirectoryGroup { get; set; }
		public bool RebootRequired { get; private set; }

		public bool EnableTracing { get; set; }
		public string TracingPath { get; private set; }

		public bool NativeMode
		{
			get
			{
				return !ActiveDirectoryMode;
			}
		}

	//	public bool UseDomainAuth 
	//	{
	//		get
	//		{
	//			return ActiveDirectoryMode;
		//
	//		}
	//		set
	//		{
	//			ActiveDirectoryMode = value;
	//		}
	//	}

		private void NotifyPropertiesChanged()
		{
			OnPropertyChanged(@"HomeDomain");
			OnPropertyChanged(@"AuthDisabled");
			OnPropertyChanged(@"UseDomainAuth");
			OnPropertyChanged(@"ActiveDirectoryMode");
			OnPropertyChanged(@"ActiveDirectoryGroup");
			OnPropertyChanged(@"NativeMode");
			OnPropertyChanged(@"Updating");
			OnPropertyChanged(@"EnableTracing");
			OnPropertyChanged(@"TracingPath");
		}


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

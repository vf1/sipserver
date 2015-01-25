using System;
using System.Text;
using System.Windows.Input;

namespace ControlPanel
{
	public class Commands
	{
		public static RoutedUICommand Connect { get; private set; }
		public static RoutedUICommand ConnectAs { get; private set; }
		public static RoutedUICommand CopyAll { get; private set; }
		public static RoutedUICommand Close { get; private set; }
		public static RoutedUICommand ApplySettings { get; private set; }
		public static RoutedUICommand ResetSettings { get; private set; }
		public static RoutedUICommand AddUser { get; private set; }
		public static RoutedUICommand RemoveUser { get; private set; }
		public static RoutedUICommand EditUser { get; private set; }
		public static RoutedUICommand ChangeUserPassword { get; private set; }
		public static RoutedUICommand ChangeAdminPassword { get; private set; }
		public static RoutedUICommand ServiceControl { get; private set; }
		public static RoutedUICommand OpenService { get; private set; }
		public static RoutedUICommand StartService { get; private set; }
		public static RoutedUICommand StopService { get; private set; }
		public static RoutedUICommand ShellExecute { get; private set; }
		public static RoutedUICommand ConfigTurnServer { get; private set; }
		public static RoutedUICommand ValidateXml { get; private set; }
		public static RoutedUICommand LoadXml { get; private set; }
		public static RoutedUICommand LoadDefaultXml { get; private set; }
		public static RoutedUICommand SaveXml { get; private set; }
		public static RoutedUICommand AddVoipProvider { get; private set; }
		public static RoutedUICommand RemoveVoipProvider { get; private set; }


		static Commands()
		{
			Connect = new RoutedUICommand("Connect", "Connect", typeof(Commands));
			ConnectAs = new RoutedUICommand("Connect", "Connect", typeof(Commands));
			CopyAll = new RoutedUICommand("Copy", "Copy", typeof(Commands));
			Close = ApplicationCommands.Close;
			ApplySettings = new RoutedUICommand("Apply", "Apply", typeof(Commands));
			ResetSettings = new RoutedUICommand("Reset", "Reset", typeof(Commands));
			AddUser = new RoutedUICommand("Add", "Add", typeof(Commands));
			RemoveUser = new RoutedUICommand("Remove", "Remove", typeof(Commands));
			EditUser = new RoutedUICommand("Edit", "Edit", typeof(Commands));
			ChangeUserPassword = new RoutedUICommand("New Password", "NewPassword", typeof(Commands));
			ChangeAdminPassword = new RoutedUICommand("Change Password", "ChangePassword", typeof(Commands));
			ServiceControl = new RoutedUICommand("Service Control", "ServiceControl", typeof(Commands));
			OpenService = new RoutedUICommand("Open", "OpenService", typeof(Commands));
			StartService = new RoutedUICommand("Start", "StartService", typeof(Commands));
			StopService = new RoutedUICommand("Stop", "StopService", typeof(Commands));
			ShellExecute = new RoutedUICommand("Shell Execute", "ShellExecute", typeof(Commands));
			ConfigTurnServer = new RoutedUICommand("Configure", "ConfigTurnServer", typeof(Commands));
			ValidateXml = new RoutedUICommand("Validate", "Validate Configuration", typeof(Commands), new InputGestureCollection() { new KeyGesture(Key.T, ModifierKeys.Control), });
			LoadXml = new RoutedUICommand("Load", "Load Configuration", typeof(Commands));
			LoadDefaultXml = new RoutedUICommand("Load Default", "Load Default Configuration", typeof(Commands));
			SaveXml = new RoutedUICommand("Save", "Save Configuration", typeof(Commands), new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control), });
			AddVoipProvider = new RoutedUICommand("Add", "Add", typeof(Commands));
			RemoveVoipProvider = new RoutedUICommand("Remove", "Remove", typeof(Commands));
		}
	}
}

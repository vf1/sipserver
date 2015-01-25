using System;
using System.Net;
using System.ServiceModel;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Sip.Server.Users;

namespace Sip.Server.WcfService
{
	[ServiceContract(Namespace = "http://officesip.com/server.control", CallbackContract = typeof(IWcfServiceCallback))]
	public interface IWcfService
	{
		[OperationContract]
		WcfConfiguration GetConfigurations();
		
		[OperationContract]
		void SetConfigurations(WcfConfiguration configurations);



		[OperationContract]
		int GetUsersCount(string id);
		
		[OperationContract]
		IList<WcfUser> GetUsers(string id, int startIndex, int count, out int overallCount);



		[OperationContract]
		void AddUser(string usersId, WcfUser user, string password);
		
		[OperationContract]
		void UpdateUser(string usersId, WcfUser user);
		
		[OperationContract]
		void RemoveUser(string usersId, string name);
		
		[OperationContract]
		void SetUserPassword(string usersId, string name, string password);



		[OperationContract]
		IEnumerable<WcfVoipProvider> GetVoipProviders();

		[OperationContract]
		void AddVoipProvider(WcfVoipProvider provider);
	
		[OperationContract]
		void RemoveVoipProvider(string username, string hostname);



		[OperationContract]
		void Ping();

		[OperationContract]
		Version GetVersion();


		[OperationContract]
		string GetDefaultXmlConfiguration();

		[OperationContract]
		string GetXmlConfiguration();

		[OperationContract]
		string[] SetXmlConfiguration(string xml);
		
		[OperationContract]
		string[] ValidateXmlConfiguration(string xml);



		[OperationContract]
		WcfTurnConfiguration GetTurnConfigurations();
		
		[OperationContract]
		void SetTurnConfigurations(WcfTurnConfiguration configurations);

		[OperationContract]
		void SetAdministratorPassword(string newPassword);
	}
}

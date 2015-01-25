using System.ServiceModel;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Net;
using Sip.Server.Users;

namespace Sip.Server.WcfService
{
	interface IWcfServiceCallback
	{
		[OperationContract(IsOneWay = true)]
		void AvailabilityChanged(string name, int availability);

		[OperationContract(IsOneWay = true)]
		void NewClient();

		[OperationContract(IsOneWay = true)]
		void UsersReset(string usersId);

		[OperationContract(IsOneWay = true)]
		void UserAddedOrUpdated(string usersId, WcfUser user);

		[OperationContract(IsOneWay = true)]
		void UserRemoved(string usersId, string name);

		[OperationContract(IsOneWay = true)]
		void VoipProviderUpdated(WcfVoipProvider info);
	}
}

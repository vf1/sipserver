using System.ServiceProcess;
using System.Threading;

namespace Sip.Server
{
	class Service : ServiceBase
	{
		public const string Name = "OfficeSIP Server";
		private Server server;

		public Service()
		{
			this.ServiceName = Name;
		}

		public void Emulate()
		{
			server = new Server();
			Thread.Sleep(Timeout.Infinite);
			server.Dispose();
			server = null;
		}

		protected override void OnStart(string[] args)
		{
			RequestAdditionalTime(2 * 60 * 1000);
			server = new Server();
		}
		protected override void OnStop()
		{
			RequestAdditionalTime(30 * 1000);
			OnShutdown();
		}
		protected override void OnShutdown()
		{
			server.Dispose();
			server = null;
		}
	}
}

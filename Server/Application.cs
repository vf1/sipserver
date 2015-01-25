using System.ServiceProcess;

namespace Sip.Server
{
	static class Application
	{
		static void Main(string[] args)
		{
			bool b = true;

			if (args.Length == 1)
				switch (char.Parse(args[0]))
				{
					case 'd':
						new Service().Emulate();
						b = false;
						break;
				}


			if (b == true)
				ServiceBase.Run(new Service());
		}
	}
}
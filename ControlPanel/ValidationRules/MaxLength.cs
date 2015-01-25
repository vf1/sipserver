using System;

namespace ControlPanel
{
	public static class MaxLength
	{
		static MaxLength()
		{
			Username = 128;
			DisplayName = 128;
			Email = 128;
			Password = 64;
			Port = 5;
			Hostname = 255;
			IPEndPoint = 64;
			Uri = 128;
		}

		public static int Username { get; private set; }
		public static int DisplayName { get; private set; }
		public static int Email { get; private set; }
		public static int Password { get; private set; }
		public static int Port { get; private set; }
		public static int Hostname { get; private set; }
		public static int IPEndPoint { get; private set; }
		public static int Uri { get; private set; }
	}
}

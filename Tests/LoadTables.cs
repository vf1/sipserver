using System;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using Sip.Server;
using Sip.Message;
using Http.Message;
using SocketServers;

namespace Test
{
	[SetUpFixture]
	public class LoadTables
	{
		[SetUp]
		public void SetUp()
		{
			{
				var dfa = new SipMessageReader();
				dfa.LoadTables(@"..\..\..\Server\dll\Sip.Message.dfa");
				dfa.SetDefaultValue();
				dfa.Parse(new byte[] { 0 }, 0, 1);
			}
			{
				HttpMessageReader.LoadTables(@"..\..\..\Server\dll\");

				var dfa = new HttpMessageReader();
				dfa.SetDefaultValue();
				dfa.Parse(new byte[] { 0 }, 0, 1);
			}
		}
	}
}

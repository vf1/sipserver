using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using Sip.Server;
using Sip.Message;
using Http.Message;
using SocketServers;
using Server.Http;

namespace Test
{
	[TestFixture]
	class ConnectionTest
	{
		private ServersManager<Connection> serversManager;
		private ServerAsyncEventArgs[] args;

		public ConnectionTest()
		{
			serversManager = new ServersManager<Connection>(new ServersManagerConfig());

			// create offset in tests
			//
			args = new ServerAsyncEventArgs[10];
			for (int i = 0; i < args.Length; i++)
			{
				args[i] = EventArgsManager.Get();
				//= serversManager.BuffersPool.Get();
				args[i].AllocateBuffer();
			}
		}

		[Test]
		public void n1_It_should_glue_small_message_without_body()
		{
			var message1 = "REGISTER sip:officesip.local SIP/2.0\r\n\r\n";
			Validate(message1, 1024, FromTo(1, message1.Length, 1));

			var message2 = "REGISTER sip:officesip.local SIP/2.0\r\nContent-Length: 0\r\n\r\n";
			Validate(message2, 1024, FromTo(1, message2.Length, 1));
		}

		[Test]
		public void n2_It_should_split_small_message_without_body()
		{
			var message = "REGISTER sip:officesip.local SIP/2.0\r\n\r\n";
			Validate(message, 16384, FromTo(message.Length + 1, ServerAsyncEventArgs.DefaultSize, 73));
		}

		[Test]
		public void n3_It_should_glue_small_message_with_mini_body()
		{
			var message = "REGISTER sip:officesip.local SIP/2.0\r\nContent-Length: 10\r\n\r\n0123456789";
			Validate(message, 1024, FromTo(1, message.Length, 1));
		}

		[Test]
		public void n4_It_should_split_small_message_with_mini_body()
		{
			var message = "REGISTER sip:officesip.local SIP/2.0\r\nContent-Length: 10\r\n\r\n0123456789";
			Validate(message, 16384, FromTo(message.Length + 1, ServerAsyncEventArgs.DefaultSize, 73));
		}

		[Test]
		public void n5_It_should_glue_small_message_with_small_body()
		{
			int bodySize = ServerAsyncEventArgs.DefaultSize * 3 / 2;

			var message = "REGISTER sip:officesip.local SIP/2.0\r\nContent-Length: "
				+ bodySize + "\r\n\r\n" + GenerateBody(bodySize);

			Validate(message, 512, FromTo(1, ServerAsyncEventArgs.DefaultSize, 11));
			//Validate(message, 512, FromTo(34, ServerAsyncEventArgs.DefaultSize, 11));
		}

		[Test]
		public void n6_It_should_glue_small_message_with_big_body()
		{
			int bodySize = ServerAsyncEventArgs.DefaultSize * 5;

			var message = "REGISTER sip:officesip.local SIP/2.0\r\nContent-Length: "
				+ bodySize + "\r\n\r\n" + GenerateBody(bodySize);

			Validate(message, 512, FromTo(1, ServerAsyncEventArgs.DefaultSize, 23));
		}

		[Test]
		public void n7_It_should_glue_big_message_without_body()
		{
			var message = "REGISTER sip:officesip.local SIP/2.0\r\n";
			var header = "Custom-Header: 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789\r\n";
			for (int i = message.Length + header.Length; i < Connection.MaximumHeadersSize; i += header.Length)
				message += header;
			message += "\r\n";

			Validate(message, 128, FromTo(1, ServerAsyncEventArgs.DefaultSize, 83));
		}

		[Test]
		public void n8_It_should_glue_big_message_with_big_body()
		{
			int bodySize = ServerAsyncEventArgs.DefaultSize * 5;

			var header = "Custom-Header: 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789\r\n";
			var message = "REGISTER sip:officesip.local SIP/2.0\r\nContent-Length: " + bodySize + "\r\n";
			for (int i = message.Length + header.Length; i < Connection.MaximumHeadersSize; i += header.Length)
				message += header;
			message += "\r\n" + GenerateBody(bodySize);

			Validate(message, 128, FromTo(1, ServerAsyncEventArgs.DefaultSize, 83));
		}

		[Test]
		public void n9_It_should_skip_keepalive()
		{
			var message1 = "REGISTER sip:officesip.local SIP/2.0\r\n\r\n";
			Validate(message1, true, 4096, FromTo(1, ServerAsyncEventArgs.DefaultSize, 10), false);
		}

		[Test]
		public void nA_It_should_parse_http_messages()
		{
			var message1 = "GET / HTTP/1.0\r\n\r\n";
			new ValidateClass(message1, 4096, FromTo(1, ServerAsyncEventArgs.DefaultSize, 10));

			int bodySize = ServerAsyncEventArgs.DefaultSize * 5;
			var message2 = "GET / HTTP/1.0\r\nContent-Length: " + bodySize + "\r\n\r\n" + GenerateBody(bodySize);
			new ValidateClass(message2, 4096, FromTo(1, ServerAsyncEventArgs.DefaultSize, 10));
		}

		[Test]
		public void nB_It_should_parse_websocket_ping()
		{
			ValidateWebsocket(FromTo(1, 150, 7), 4096);
		}

		[Test]
		public void nC_It_should_parse_ajax_messages_without_body()
		{
			var sip = "REGISTER sip:officesip.local SIP/2.0\r\n\r\n";
			var http = "GET /ajax.websocket?xxx=yyy HTTP/1.0\r\nContent-Length: " + sip.Length + "\r\n\r\n";

			new ValidateClass(http, sip, 4096, FromTo(1, ServerAsyncEventArgs.DefaultSize, 7));
		}

		[Test]
		public void nD_It_should_parse_ajax_messages_with_body()
		{
			var sip = "REGISTER sip:officesip.local SIP/2.0\r\nContent-Length: 10\r\n\r\n1234567890";
			var http = "GET /ajax.websocket HTTP/1.1\r\nContent-Length: " + sip.Length + "\r\n\r\n";

			new ValidateClass(http, sip, 4096, FromTo(1, ServerAsyncEventArgs.DefaultSize, 7));
		}

		[Test]
		public void nE_It_should_parse_ajax_messages_example()
		{
			var http = "POST /ajax.websocket?previousRequest=null&currentRequest=1336601401188 HTTP/1.1\r\n" +
					"Host: localhost\r\n" +
					"Connection: keep-alive\r\n" +
					"Content-Length: 218\r\n" +
					"Origin: http://localhost\r\n" +
					"X-Requested-With: XMLHttpRequest\r\n" +
					"User-Agent: Mozilla/5.0 (Windows NT 6.0) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.168 Safari/535.19\r\n" +
					"Content-Type: application/x-www-form-urlencoded;charset=UTF-8\r\n" +
					"Accept: text/plain, */*; q=0.01\r\n" +
					"Referer: http://localhost/test-socket.html\r\n" +
					"Accept-Encoding: gzip,deflate,sdch\r\n" +
					"Accept-Language: en-US,en;q=0.8\r\n" +
					"Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3\r\n" +
					"Cookie: sessionid=1\r\n" +
					"\r\n";

			var sip = "REGISTER sip:officesip.local SIP/2.0\r\n" +
					"Via: SIP/2.0/WS 127.0.0.1:1800\r\n" +
					"Max-Forwards: 70\r\n" +
					"From: <sip:sipjs@officesip.local>\r\n" +
					"To: <sip:sipjs@officesip.local>\r\n" +
					"Call-ID: 16743485a6e45407e903f75571a3a7af9\r\n" +
					"CSeq: 2 REGISTER\r\n" +
					"\r\n";

			int length = http.Length + sip.Length;

			new ValidateClass(http, sip, 2, FromTo(length, length, 1));
		}

		private void Validate(string messageText, int messagesCount, IEnumerable<int> splitSizes)
		{
			Validate(messageText, false, messagesCount, splitSizes, false);
			Validate(messageText, true, messagesCount, splitSizes, true);
			Validate(messageText, false, messagesCount, splitSizes, false, true);
		}

		private void Validate(string messageText, bool addKeepAlive, int messagesCount, IEnumerable<int> splitSizes, bool detachBuffer)
		{
			Validate(messageText, addKeepAlive, messagesCount, splitSizes, detachBuffer, false);
		}

		class Threads
			: IDisposable
		{
			private bool repeat;
			private bool closeConnection;
			private ServerAsyncEventArgs e;
			private Connection connection;
			private AutoResetEvent done;
			private AutoResetEvent[] release;
			private const int maxThread = 16;
			private int index;

			public Threads(Connection connection)
			{
				this.connection = connection;
				this.done = new AutoResetEvent(false);

				this.release = new AutoResetEvent[maxThread];
				for (int i = 0; i < this.release.Length; i++)
				{
					this.release[i] = new AutoResetEvent(false);
					ThreadPool.QueueUserWorkItem((x) => { release[(int)x].WaitOne(); }, i);
				}
			}

			public void Dispose()
			{
				done.Close();

				for (int i = 0; i < release.Length; i++)
					release[i].Set();

				Thread.Sleep(100);

				for (int i = 0; i < release.Length; i++)
					release[i].Close();
			}

			public bool Proccess(ref ServerAsyncEventArgs e, out bool closeConnection)
			{
				this.e = e;

				if (index < maxThread)
				{
					release[index].Set();
					ThreadPool.QueueUserWorkItem(Proccess, index);
					done.WaitOne();
				}
				else
				{
					Proccess();
				}

				index = (index + 1) % (maxThread * 2);

				e = this.e;
				closeConnection = this.closeConnection;
				return repeat;
			}

			private void Proccess()
			{
				repeat = connection.Proccess(ref e, out closeConnection);
			}

			private void Proccess(object p)
			{
				Proccess();

				done.Set();
				release[(int)p].WaitOne();
			}
		}

		private void Validate(string messageText, bool addKeepAlive, int messagesCount, IEnumerable<int> splitSizes, bool detachBuffer, bool websocket)
		{
			var message = Encoding.UTF8.GetBytes(messageText);

			var expected = new SipMessageReader();
			expected.SetDefaultValue();
			int parsed = expected.Parse(message, 0, message.Length);
			expected.SetArray(message);
			if (expected.ContentLength < 0)
				expected.ContentLength = 0;
			var expectedUri = expected.RequestUri.Value.ToString();
			var content = Encoding.UTF8.GetString(message, parsed, message.Length - parsed);

			if (addKeepAlive)
				message = Encoding.UTF8.GetBytes("\r\n\r\n" + messageText + "\r\n\r\n\r\n\r\n\r\n\r\n");

			var extra = new byte[0];
			if (websocket)
				extra = PrepareForWebsocket(message);

			var stream = CreateStream(messagesCount, extra, message);
			int headersLength = messageText.IndexOf("\r\n\r\n") + 4;
			var headersText = messageText.Substring(0, headersLength);

			int oldUsedBuffers = EventArgsManager.Created - EventArgsManager.Queued;

			ServerAsyncEventArgs e = null;
			using (var connection = new Connection())
			using (var threads = new Threads(connection))
			{
				if (websocket)
					connection.UpgradeToWebsocket();

				foreach (int splitSize in splitSizes)
				{
					int realMessageCount = 0;

					for (int offset = 0; offset < stream.Length; offset += splitSize)
					{
						var info = "Split by: " + splitSize + "; Message #: " + realMessageCount +
							"\r\nMessage Sizes: " + message.Length + " " + (message.Length - content.Length) + " " + content.Length;

						if (e == null)
							e = EventArgsManager.Get();

						e.BytesTransferred = Math.Min(splitSize, stream.Length - offset);
						Buffer.BlockCopy(stream, offset, e.Buffer, e.Offset, e.BytesTransferred);

						bool closeConnection;
						for (bool repeat = true; repeat; )
						{
							//repeat = connection.Proccess(ref e, out closeConnection);
							repeat = threads.Proccess(ref e, out closeConnection);

							Assert.IsFalse(closeConnection, info);

							if (connection.IsMessageReady)
							{
								var actualHeaders = Encoding.UTF8.GetString(connection.Header.Array, connection.Header.Offset, connection.Header.Count);

								Assert.AreEqual(expected.Method, connection.Reader.Method, info);
								Assert.AreEqual(expectedUri, connection.Reader.RequestUri.Value.ToString(), info);

								Assert.AreEqual(headersLength, connection.Header.Count);
								Assert.AreEqual(headersText, actualHeaders);

								Assert.AreEqual(expected.ContentLength, connection.Content.Count);
								if (expected.ContentLength > 0)
									Assert.AreEqual(content, Encoding.UTF8.GetString(connection.Content.Array, connection.Content.Offset, connection.Content.Count), info);

								BufferHandle handle = new BufferHandle();
								if (detachBuffer)
									handle = connection.Dettach(ref e);

								connection.ResetState();
								realMessageCount++;

								if (detachBuffer)
									handle.Free();
							}
						}
					}

					EventArgsManager.Put(ref e);

					Assert.AreEqual(messagesCount, realMessageCount);
				}
			}

			Assert.AreEqual(oldUsedBuffers, EventArgsManager.Created - EventArgsManager.Queued);
		}

		private static void AreEqual(byte[] bytes, ArraySegment<byte> segment)
		{
			Assert.AreEqual(bytes.Length, segment.Count);
			for (int i = 0; i < bytes.Length; i++)
				Assert.AreEqual(bytes[i], segment.Array[segment.Offset + i]);
		}

		private static byte[] PrepareForWebsocket(byte[] message)
		{
			return PrepareForWebsocket(message, Opcodes.Text);
		}

		private static byte[] PrepareForWebsocket(byte[] message, Opcodes opcode)
		{
			var wsHeader = new WebSocketHeader()
			{
				Fin = true,
				Opcode = opcode,
				PayloadLength = message.Length,
				Mask = true,
				MaskingKey0 = 0x12,
				MaskingKey1 = 0x34,
				MaskingKey2 = 0x56,
				MaskingKey3 = 0x78,
			};

			var extra = new byte[wsHeader.GetHeaderLength()];
			wsHeader.GenerateHeader(new ArraySegment<byte>(extra));

			if (wsHeader.Mask)
				wsHeader.MaskData(message, 0, message.Length);

			return extra;
		}

		private static void Unmask(byte[] message)
		{
			var wsHeader = new WebSocketHeader()
			{
				Mask = true,
				MaskingKey0 = 0x12,
				MaskingKey1 = 0x34,
				MaskingKey2 = 0x56,
				MaskingKey3 = 0x78,
			};

			wsHeader.MaskData(message, 0, message.Length);
		}

		class ValidateClass
		{
			private string httpHeader;
			private string httpContent;
			private string sipHeader;
			private string sipContent;
			private HttpMessageReader httpExpected;
			private SipMessageReader sipExpected;

			public ValidateClass(string httpMessage, int messagesCount, IEnumerable<int> splitSizes)
				: this(httpMessage, null, messagesCount, splitSizes)
			{
			}

			public ValidateClass(string httpMessage, string sipMessage, int messagesCount, IEnumerable<int> splitSizes)
			{
				if (httpMessage != null)
					ParseHtppMessage(httpMessage);
				if (sipMessage != null)
					ParseSipMessage(sipMessage);

				var stream = CreateStream(messagesCount, httpMessage, sipMessage);

				int oldUsedBuffers = EventArgsManager.Created - EventArgsManager.Queued;

				ServerAsyncEventArgs e = null;
				using (var connection = new Connection())
				{
					foreach (int splitSize in splitSizes)
					{
						int realMessageCount = 0;

						for (int offset = 0; offset < stream.Length; offset += splitSize)
						{
							var details = string.Format("Split by: {0}; Message #: {1}", splitSize, realMessageCount);

							if (e == null)
								e = EventArgsManager.Get();

							e.BytesTransferred = Math.Min(splitSize, stream.Length - offset);
							Buffer.BlockCopy(stream, offset, e.Buffer, e.Offset, e.BytesTransferred);

							bool closeConnection;
							for (bool repeat = true; repeat; )
							{
								repeat = connection.Proccess(ref e, out closeConnection);

								Assert.IsFalse(closeConnection, details);

								if (connection.IsMessageReady)
								{
									if (httpMessage != null)
										ValidateHttp(connection, details);
									if (sipMessage != null)
										ValidateSip(connection);

									connection.ResetState();
									realMessageCount++;
								}
							}
						}

						EventArgsManager.Put(ref e);

						Assert.AreEqual(messagesCount, realMessageCount);
					}
				}

				Assert.AreEqual(oldUsedBuffers, EventArgsManager.Created - EventArgsManager.Queued);
			}

			private void ValidateHttp(Connection connection, string details)
			{
				Assert.AreEqual(httpExpected.Method, connection.HttpReader.Method, details);
				Assert.AreEqual(httpExpected.RequestUri.ToString(), connection.HttpReader.RequestUri.ToString(), details);

				if (connection.Mode != Connection.Modes.Ajax)
				{
					Assert.AreEqual(httpHeader.Length, connection.Header.Count, details);
					Assert.AreEqual(httpHeader, Encoding.UTF8.GetString(connection.Header.Array, connection.Header.Offset, connection.Header.Count), details);

					Assert.AreEqual(httpExpected.ContentLength, connection.Content.Count);
					if (httpExpected.ContentLength > 0)
						Assert.AreEqual(httpContent, Encoding.UTF8.GetString(connection.Content.Array, connection.Content.Offset, connection.Content.Count));
				}
			}

			private void ValidateSip(Connection connection)
			{
				Assert.AreEqual(sipExpected.Method, connection.Reader.Method);
				Assert.AreEqual(sipExpected.RequestUri.Value.ToString(), connection.Reader.RequestUri.Value.ToString());

				Assert.AreEqual(sipHeader.Length, connection.Header.Count);
				Assert.AreEqual(sipHeader, Encoding.UTF8.GetString(connection.Header.Array, connection.Header.Offset, connection.Header.Count));

				Assert.AreEqual(sipExpected.ContentLength, connection.Content.Count);
				if (sipExpected.ContentLength > 0)
					Assert.AreEqual(sipContent, Encoding.UTF8.GetString(connection.Content.Array, connection.Content.Offset, connection.Content.Count));
			}

			private void ParseHtppMessage(string message)
			{
				var bytes = Encoding.UTF8.GetBytes(message);

				httpExpected = new HttpMessageReader();

				httpExpected.SetDefaultValue();
				int parsed = httpExpected.Parse(bytes, 0, bytes.Length);
				httpExpected.SetArray(bytes);
				if (httpExpected.ContentLength < 0)
					httpExpected.ContentLength = 0;

				httpHeader = message.Substring(0, parsed);
				httpContent = message.Substring(parsed);
			}

			private void ParseSipMessage(string message)
			{
				var bytes = Encoding.UTF8.GetBytes(message);

				sipExpected = new SipMessageReader();

				sipExpected.SetDefaultValue();
				int parsed = sipExpected.Parse(bytes, 0, bytes.Length);
				sipExpected.SetArray(bytes);
				if (sipExpected.ContentLength < 0)
					sipExpected.ContentLength = 0;

				sipHeader = message.Substring(0, parsed);
				sipContent = message.Substring(parsed);
			}

			public static byte[] CreateStream(int count, params string[] messages)
			{
				var bytes = new byte[messages.Length][];
				for (int i = 0; i < messages.Length; i++)
					bytes[i] = (messages[i] == null) ? null : Encoding.UTF8.GetBytes(messages[i]);

				return CreateStream(count, bytes);
			}

			public static byte[] CreateStream(int count, params byte[][] messages)
			{
				int totalLength = 0;
				for (int j = 0; j < messages.Length; j++)
					if (messages[j] != null)
						totalLength += messages[j].Length;

				var stream = new byte[totalLength * count];

				for (int i = 0, offset = 0; i < count; i++)
				{
					for (int j = 0; j < messages.Length; j++)
						if (messages[j] != null)
						{
							Buffer.BlockCopy(messages[j], 0, stream, offset, messages[j].Length);
							offset += messages[j].Length;
						}
				}

				return stream;
			}
		}

		private void ValidateWebsocket(IEnumerable<int> splitSizes, int messagesCount)
		{
			var ping1 = new byte[] { 1, 2, 3, 4, 5, 6, 7, };
			var ping2 = new byte[] { 8, 9, 10 };
			var ping3 = new byte[] { 11, 12, 13, 14, 15, 16, 17, 18 };
			var message = Encoding.UTF8.GetBytes("REGISTER sip:officesip.local SIP/2.0\r\n\r\n");

			var stream = CreateStream(
				messagesCount,
				PrepareForWebsocket(ping1, Opcodes.Ping), ping1,
				PrepareForWebsocket(ping2, Opcodes.Ping), ping2,
				PrepareForWebsocket(ping3, Opcodes.Ping), ping3,
				PrepareForWebsocket(message, Opcodes.Binary), message
			);

			Unmask(ping1);
			Unmask(ping2);
			Unmask(ping3);
			Unmask(message);

			ServerAsyncEventArgs e = null;
			using (var connection = new Connection())
			{
				connection.UpgradeToWebsocket();

				foreach (int splitSize in splitSizes)
				{
					int realMessageCount = 0;

					for (int offset = 0; offset < stream.Length; offset += splitSize)
					{
						var info = "Split by: " + splitSize + "; Message #: " + realMessageCount;

						if (e == null)
							e = EventArgsManager.Get();

						e.BytesTransferred = Math.Min(splitSize, stream.Length - offset);
						Buffer.BlockCopy(stream, offset, e.Buffer, e.Offset, e.BytesTransferred);

						bool closeConnection;
						for (bool repeat = true; repeat; )
						{
							repeat = connection.Proccess(ref e, out closeConnection);

							Assert.IsFalse(closeConnection, info);

							if (connection.IsMessageReady)
							{
								switch (realMessageCount % 4)
								{
									case 0:
										Assert.AreEqual(Opcodes.Ping, connection.WebSocketHeader.Opcode);
										AreEqual(ping1, connection.Content);
										break;
									case 1:
										Assert.AreEqual(Opcodes.Ping, connection.WebSocketHeader.Opcode);
										AreEqual(ping2, connection.Content);
										break;
									case 2:
										Assert.AreEqual(Opcodes.Ping, connection.WebSocketHeader.Opcode);
										AreEqual(ping3, connection.Content);
										break;
									case 3:
										Assert.AreEqual(Opcodes.Binary, connection.WebSocketHeader.Opcode);
										AreEqual(message, connection.Header);
										break;
								}

								connection.ResetState();
								realMessageCount++;
							}
						}
					}

					EventArgsManager.Put(ref e);

					Assert.AreEqual(messagesCount * 4, realMessageCount);
				}
			}
		}

		private IEnumerable<int> FromTo(int from, int to, int step)
		{
			for (int i = from; i < to; i += step)
				yield return i;
			yield return to;
		}

		//private byte[] CreateStream(int count, byte[] message, params byte[][] extra)
		//{
		//    int extraLength = 0;
		//    for (int j = 0; j < extra.Length; j++)
		//        extraLength += extra[j].Length;

		//    var stream = new byte[(extraLength + message.Length) * count];

		//    for (int i = 0; i < count; i++)
		//    {
		//        int offset = i * (extraLength + message.Length);

		//        for (int j = 0, of = 0; j < extra.Length; of += extra[j].Length, j++)
		//            Buffer.BlockCopy(extra[j], 0, stream, offset + of, extra[j].Length);
		//        Buffer.BlockCopy(message, 0, stream, offset + extraLength, message.Length);
		//    }

		//    return stream;
		//}

		private byte[] CreateStream(int count, params string[] messages)
		{
			var bytes = new byte[messages.Length][];
			for (int i = 0; i < messages.Length; i++)
				bytes[i] = Encoding.UTF8.GetBytes(messages[i]);

			return CreateStream(count, bytes);
		}

		private byte[] CreateStream(int count, params byte[][] messages)
		{
			int totalLength = 0;
			for (int j = 0; j < messages.Length; j++)
				totalLength += messages[j].Length;

			var stream = new byte[totalLength * count];

			for (int i = 0; i < count; i++)
			{
				int offset = i * totalLength;

				for (int j = 0, of = 0; j < messages.Length; of += messages[j].Length, j++)
					Buffer.BlockCopy(messages[j], 0, stream, offset + of, messages[j].Length);
			}

			return stream;
		}

		private string GenerateBody(int length)
		{
			var random = new Random();
			var builder = new StringBuilder(length);
			var chars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 
			    'd', 'e', 'f', 'g', 'h', };

			for (int i = 0; i < length; i++)
				builder.Append(chars[random.Next() % chars.Length]);

			return builder.ToString();
		}
	}
}

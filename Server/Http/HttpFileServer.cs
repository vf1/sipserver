using System;
using System.IO;
using System.Text;
using System.Timers;
using System.Collections.Generic;
using System.Security.Cryptography;
using Base.Message;
using Http.Message;
using SocketServers;
using Sip.Server.Configuration;

namespace Server.Http
{
	class HttpFileServer
		: IHttpServerAgent
	{
		struct HttpFile
		{
			public HttpFile(byte[] content, ContentType contentType)
			{
				Content = content;
				ContentType = contentType;
			}

			public readonly byte[] Content;
			public readonly ContentType ContentType;
		}

		private string path;
		private byte[] uri1;
		private byte[] uri2;
		private int loadCount;
		private Dictionary<string, HttpFile> files;
		private FileSystemWatcher watcher;
		private Timer timer;
		private const int maxFileSize = 4 * 1024 * 1024;
		private static readonly string[] defaultFiles = new string[] { "index.html", "index.htm", };

		private IHttpServer httpServer;

		public HttpFileServer(string path, string uri)
		{
			this.path = path;
			this.uri1 = string.IsNullOrEmpty(uri) ? new byte[0] : Encoding.UTF8.GetBytes(uri.Substring(0, uri.Length - 1));
			this.uri2 = Encoding.UTF8.GetBytes(uri);

			this.files = new Dictionary<string, HttpFile>();

			this.timer = new Timer(1000);
			this.timer.Elapsed += Timer_Elapsed;
			this.timer.AutoReset = false;

			this.timer.Start();
		}

		public void Dispose()
		{
			if (watcher != null)
				watcher.Dispose();
		}

		public string WwwPath
		{
			get { return path; }
			set
			{
				if (path != value)
				{
					path = value;
					LoadFiles();
				}
			}
		}

		IHttpServer IHttpServerAgent.IHttpServer
		{
			set { httpServer = value; }
		}

		HttpServerAgent.IsHandledResult IHttpServerAgent.IsHandled(HttpMessageReader httpReader)
		{
			return (string.IsNullOrEmpty(path) == false) &&
				(httpReader.RequestUri.Equals(uri1) || httpReader.RequestUri.StartsWith(uri2));
		}

		bool IHttpServerAgent.IsAuthorized(HttpMessageReader reader, ByteArrayPart username)
		{
			return true;
		}

		void IHttpServerAgent.HandleRequest(BaseConnection c, HttpMessageReader httpReader, ArraySegment<byte> httpContent)
		{
			if (httpReader.Method != Methods.Get)
			{
				var writer = new HttpMessageWriter();
				writer.WriteResponse(StatusCodes.NotAcceptable);

				httpServer.SendResponse(c, writer);
			}
			else
			{
				if (httpReader.RequestUri.Equals(uri1))
				{
					var writer = new HttpMessageWriter();

					writer.WriteStatusLine(StatusCodes.MovedPermanently);
					writer.WriteContentLength(0);
					writer.WriteLocation(c.LocalEndPoint.Protocol == ServerProtocol.Tcp, httpReader.Host.Host, httpReader.Host.Port, uri2);
					writer.WriteCRLF();

					httpServer.SendResponse(c, writer);
				}
				else
				{
					var rawFile = GetFile(httpReader);

					if (rawFile.HasValue == false)
					{
						var writer = new HttpMessageWriter();
						writer.WriteResponse(StatusCodes.NotFound);

						httpServer.SendResponse(c, writer);
					}
					else
					{
						var file = rawFile.Value;

						var writer = new HttpMessageWriter();
						writer.WriteStatusLine(StatusCodes.OK);
						writer.WriteContentType(file.ContentType);
						writer.WriteContentLength(file.Content.Length);
						writer.WriteCRLF();

						httpServer.SendResponse(c, writer);

						httpServer.SendResponse(c, new ArraySegment<byte>(file.Content));
					}
				}
			}
		}

		private void LoadFiles()
		{
			int count;
			lock (files)
			{
				files.Clear();
				count = ++loadCount;
			}

			LoadFiles(path, "/", count);
			UpdateWatcher();
		}

		private void LoadFiles(string path1, string extra, int count)
		{
			if ((string.IsNullOrEmpty(path1) == false) && Directory.Exists(path1))
			{
				foreach (var fileName in Directory.GetFiles(path1))
				{
					if (new FileInfo(fileName).Length < maxFileSize)
					{
						var file = File.ReadAllBytes(fileName);

						lock (files)
						{
							if (count != loadCount)
								return;
							files.Add(extra + Path.GetFileName(fileName),
								new HttpFile(file, HttpMessage.GetContentType(fileName, file)));
						}
					}
				}

				foreach (var directory in Directory.GetDirectories(path1))
					LoadFiles(directory, extra + Path.GetFileName(directory) + "/", count);
			}
		}

		private HttpFile? GetFile(HttpMessageReader reader)
		{
			var uri = reader.RequestUri.ToString(uri1.Length);
			if (uri == null)
				return null;

			int parameters = uri.IndexOf('?');
			if (parameters != -1)
				uri = uri.Substring(0, parameters);

			lock (files)
			{
				HttpFile file;
				if (uri.EndsWith("/") == false)
				{
					if (files.TryGetValue(uri, out file))
						return file;
				}
				else
				{
					foreach (var defaultName in defaultFiles)
						if (files.TryGetValue(uri + defaultName, out file))
							return file;
				}
			}

			return null;
		}

		private void UpdateWatcher()
		{
			lock (files)
			{
				if (watcher == null || watcher.Path != path)
				{
					if (Directory.Exists(path))
					{
						try
						{
							if (watcher != null)
								watcher.Dispose();
							watcher = new FileSystemWatcher(path);
							watcher.Changed += Watcher_Changed;
							watcher.Created += Watcher_Changed;
							watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
							watcher.IncludeSubdirectories = true;
							watcher.EnableRaisingEvents = true;
						}
						catch
						{
						}
					}
				}
			}
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			LoadFiles();
		}

		private void Watcher_Changed(object sender, FileSystemEventArgs e)
		{
			if (timer.Enabled)
				timer.Stop();
			timer.Start();
		}
	}
}

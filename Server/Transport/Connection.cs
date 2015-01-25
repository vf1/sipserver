using System;
using SocketServers;
using Sip.Message;
using Http.Message;
using Server.Http;

namespace Sip.Server
{
	public sealed class Connection
		: BaseHttpConnection
		, IDisposable
	{
		private Modes mode;
		private int keepAliveCount;

		private WebSocketHeader wsHeader;
		private bool wsWaitHeader;
		private int wsMaskIndex;

		private int httpHeaderLength;
		private int httpContentLength;

		public enum Modes
		{
			Sip,
			Http,
			WebSocket,
			Ajax,
		}

		#region static class SipReader {...}

		static class SipReader
		{
			[ThreadStatic]
			public static SipMessageReader Value;
			[ThreadStatic]
			private static int connectionId;

			public static void Update(int currentConnectionId, ArraySegment<byte> header)
			{
				connectionId = currentConnectionId;

				if (Value == null)
					Value = new SipMessageReader();

				Value.SetDefaultValue();
				Value.Parse(header.Array, header.Offset, header.Count);
				Value.SetArray(header.Array);
			}

			public static SipMessageReader GetEmpty()
			{
				connectionId = -1;

				if (Value == null)
					Value = new SipMessageReader();
				Value.SetDefaultValue();

				return Value;
			}

			public static void Invalidate()
			{
				connectionId = -1;
			}

			public static bool IsValid(int currentConnectionId)
			{
				return connectionId == currentConnectionId && Value != null;
			}

			public static bool IsInvalid(int currentConnectionId)
			{
				return connectionId != currentConnectionId || Value == null;
			}
		}

		#endregion

		#region static class HttpReader {...}

		static class HttpReaderx
		{
			[ThreadStatic]
			public static HttpMessageReader Value;
			[ThreadStatic]
			private static int connectionId;

			public static void Update(int currentConnectionId, ArraySegment<byte> header)
			{
				connectionId = currentConnectionId;

				if (Value == null)
					Value = new HttpMessageReader();

				Value.SetDefaultValue();
				Value.Parse(header.Array, header.Offset, header.Count);
				Value.SetArray(header.Array);
			}

			public static HttpMessageReader GetEmpty()
			{
				connectionId = -1;

				if (Value == null)
					Value = new HttpMessageReader();
				Value.SetDefaultValue();

				return Value;
			}

			public static void Invalidate()
			{
				connectionId = -1;
			}

			public static bool IsValid(int currentConnectionId)
			{
				return connectionId == currentConnectionId && Value != null;
			}

			public static bool IsInvalid(int currentConnectionId)
			{
				return connectionId != currentConnectionId || Value == null;
			}
		}

		#endregion

		public Connection()
		{
			mode = Modes.Sip;

			ResetParser(ResetReason.ResetStateCalled);
			wsWaitHeader = true;
			wsMaskIndex = 0;
		}

		void IDisposable.Dispose()
		{
			base.Dispose();
		}

		public Modes Mode
		{
			get { return mode; }
		}

		public SipMessageReader Reader
		{
			get
			{
				if (SipReader.IsInvalid(Id))
					SipReader.Update(Id, Header);
				return SipReader.Value;
			}
		}

		public override HttpMessageReader HttpReader
		{
			get
			{
				if (HttpReaderx.IsInvalid(Id))
					HttpReaderx.Update(Id, base.Header);
				return HttpReaderx.Value;
			}
		}

		public WebSocketHeader WebSocketHeader
		{
			get { return wsHeader; }
		}

		public bool IsSipWebSocket
		{
			get { return wsHeader.Opcode == Opcodes.Text || wsHeader.Opcode == Opcodes.Binary; }
		}

		public BufferHandle Dettach(ref ServerAsyncEventArgs e)
		{
			ArraySegment<byte> segment1, segment2;
			base.Dettach(ref e, out segment1, out segment2);

			return new BufferHandle(segment1, segment2);
		}

		public void UpgradeToWebsocket()
		{
			mode = Modes.WebSocket;
		}

		protected override void ResetParser(ResetReason reason)
		{
			keepAliveCount = 0;

			SipReader.Invalidate();

			if (mode == Modes.Ajax)
			{
				if (reason == ResetReason.ResetStateCalled)
					mode = Modes.Http;
			}
		}

		protected override void MessageReady()
		{
			if (mode == Modes.Sip || mode == Modes.WebSocket || mode == Modes.Ajax)
				if (SipReader.IsValid(Id))
					SipReader.Value.SetArray(base.Header.Array);

			if (mode == Modes.Http || mode == Modes.Ajax)
				if (HttpReaderx.IsValid(Id))
					HttpReaderx.Value.SetArray(base.Header.Array);

			wsMaskIndex = 0;
			wsWaitHeader = true;
		}

		protected override void PreProcessRaw(ArraySegment<byte> data)
		{
			if (mode == Modes.WebSocket)
			{
				if (wsWaitHeader == false)
				{
					if (wsHeader.Mask)
						wsHeader.UnmaskData(data.Array, data.Offset,
							Math.Min(data.Count, wsHeader.PayloadLength - wsMaskIndex), ref wsMaskIndex);
				}
			}
		}

		protected override ParseResult Parse(ArraySegment<byte> data)
		{
			switch (mode)
			{
				case Modes.Sip:
					{
						if (keepAliveCount != 0)
						{
							return SkipKeepAlive(data);
						}
						else
						{
							var sipReader = SipReader.GetEmpty();

							int headerLength = sipReader.Parse(data.Array, data.Offset, data.Count);

							if (sipReader.IsFinal)
								return ParseResult.HeaderDone(headerLength, sipReader.HasContentLength ? sipReader.ContentLength : 0);

							if (sipReader.IsError)
							{
								if (IsKeepAliveByte(data.Array[data.Offset], 0))
									return SkipKeepAlive(data);
								else
								{
									mode = Modes.Http;
									goto case Modes.Http;
								}
							}

							return ParseResult.NotEnoughData();
						}
					}

				case Modes.Http:
					{
						var httpReader = HttpReaderx.GetEmpty();

						httpHeaderLength = httpReader.Parse(data.Array, data.Offset, data.Count);

						if (httpReader.IsFinal)
						{
							httpContentLength = httpReader.HasContentLength ? httpReader.ContentLength : 0;

							if (AjaxWebsocket.IsAjax(httpReader, data.Array))
							{
								mode = Modes.Ajax;
								goto case Modes.Ajax;
							}

							return ParseResult.HeaderDone(httpHeaderLength, httpContentLength);
						}

						if (httpReader.IsError)
							return ParseResult.Error();

						return ParseResult.NotEnoughData();
					}

				case Modes.WebSocket:
					{
						if (wsWaitHeader)
						{
							int used = wsHeader.Parse(data);

							if (wsHeader.IsDone)
								wsWaitHeader = false;

							return ParseResult.Skip(used);
						}
						else
						{
							if (IsSipWebSocket == false)
							{
								return ParseResult.HeaderDone(0, wsHeader.PayloadLength);
							}
							else
							{
								var sipReader = SipReader.GetEmpty();

								int headerLength = sipReader.Parse(data.Array, data.Offset, data.Count);

								if (sipReader.IsFinal)
									return ParseResult.HeaderDone(headerLength, sipReader.HasContentLength ? sipReader.ContentLength : 0);

								if (sipReader.IsError)
									return ParseResult.Error();

								return ParseResult.NotEnoughData();
							}
						}
					}

				case Modes.Ajax:
					{
						if (httpContentLength > 0)
						{
							if (httpHeaderLength < data.Count)
							{
								var sipReader = SipReader.GetEmpty();

								int sipHeaderLength = sipReader.Parse(data.Array, data.Offset + httpHeaderLength, data.Count - httpHeaderLength);

								if (sipReader.IsFinal)
									return ParseResult.HeaderDone(httpHeaderLength + sipHeaderLength, sipReader.HasContentLength ? sipReader.ContentLength : 0);

								if (sipReader.IsError || httpContentLength <= data.Count - httpHeaderLength)
									return ParseResult.Error();
							}

							HttpReaderx.Invalidate();

							return ParseResult.NotEnoughData();
						}
						else
						{
							return ParseResult.HeaderDone(httpHeaderLength, 0);
						}
					}

				default:
					throw new NotImplementedException();
			}
		}

		public new ArraySegment<byte> Header
		{
			get
			{
				return (mode != Modes.Ajax)
					? base.Header
					: new ArraySegment<byte>(base.Header.Array, base.Header.Offset + httpHeaderLength, base.Header.Count - httpHeaderLength);
			}
		}

		private ParseResult SkipKeepAlive(ArraySegment<byte> data)
		{
			int processed;
			for (processed = 0; keepAliveCount < 4 && processed < data.Count; processed++)
			{
				if (IsKeepAliveByte(data.Array[data.Offset + processed], keepAliveCount) == false)
					return ParseResult.Error();

				keepAliveCount++;
			}

			if (keepAliveCount == 4)
				keepAliveCount = 0;

			return ParseResult.Skip(processed);
		}

		private static bool IsKeepAliveByte(byte byte1, int count)
		{
			if ((count & 1) == 0)
				return byte1 == 13;
			return byte1 == 10;
		}
	}
}

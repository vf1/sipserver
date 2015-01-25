using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	public class Trunk
	{
		public enum States
		{
			Initiliazed,
			WaitingDns,
			Waiting200or401,
			Waiting200,
			Connected,
			Disconnecting,
			Disconnected,
			Error,
		}

		#region struct Dialogs {...}

		struct Dialogs
		{
			public Dialogs(Dialog internal1, Dialog external)
			{
				Internal = internal1;
				External = external;
			}

			public readonly Dialog Internal;
			public readonly Dialog External;
		}

		#endregion

		#region struct UsedCSeq {...}

		struct UsedCSeq
		{
			public UsedCSeq(int hash, int cseq)
			{
				Hash = hash;
				CSeq = cseq;
			}

			public readonly int Hash;
			public readonly int CSeq;

			public static int CalculateHash(ByteArrayPart callId, int originalCSeq)
			{
				return callId.GetHashCode() ^ originalCSeq * 181;
			}
		}

		#endregion

		private States state;
		private string errorMessage;
		private int nonceCount;

		private ByteArrayPart nonce;
		private ByteArrayPart opaque;
		private AuthQops qop;
		private HeaderNames authHeader;

		//private readonly Dictionary<int, Dialogs> dialogs;
		private readonly Dictionary<int, Dialog> dialogs1;
		private readonly Dictionary<int, Dialog> dialogs2;
		private readonly object sync;

		private int inviteCseq;
		private int cseq;

		private UsedCSeq[] usedCSeqs;
		private int usedCSeqsIndex;

		public event Action<Trunk> Changed;

		public readonly int Id;

		public readonly string Username;
		public readonly string Hostname;
		public readonly string OutgoingProxy;

		public readonly ByteArrayPart DisplayName;
		public readonly ByteArrayPart Domain;
		public readonly ByteArrayPart Realm;
		public readonly ByteArrayPart Uri;
		public readonly ByteArrayPart RequestUri;
		public readonly ByteArrayPart ForwardCallToUri;
		public readonly ByteArrayPart AuthenticationId;
		public readonly ByteArrayPart Password;
		public readonly int RegisterDuration;
		public readonly int RegisterAfterErrorTimeout;

		public IPEndPoint LocalEndPoint { get; private set; }
		public Transports Transport { get; private set; }
		public IPEndPoint ServerEndPoint { get; private set; }

		public Trunk(string displayName, string hostname, string username, Transports transport, IPEndPoint localEndPoint, string outgoingProxy, string authId, string password, string localUri, int registerAfterErrorTimeout)
		{
			sync = new object();
			state = States.Initiliazed;
			nonce.SetDefaultValue();
			opaque.SetDefaultValue();
			errorMessage = "Connecting...";

			Username = username;
			Hostname = hostname;
			OutgoingProxy = string.IsNullOrEmpty(outgoingProxy) ? hostname : outgoingProxy;

			DisplayName = new ByteArrayPart(string.IsNullOrEmpty(displayName) ? username : displayName);
			Domain = new ByteArrayPart(hostname);
			Realm = Domain;
			Uri = new ByteArrayPart("sip:" + username + "@" + hostname);
			RequestUri = new ByteArrayPart("sip:" + hostname);
			Transport = transport;
			LocalEndPoint = (localEndPoint.Address == IPAddress.None) ? new IPEndPoint(IPAddress.Any, 0) : localEndPoint;
			AuthenticationId = new ByteArrayPart(string.IsNullOrEmpty(authId) ? username : authId);
			Password = new ByteArrayPart(password);
			RegisterDuration = 600;
			RegisterAfterErrorTimeout = registerAfterErrorTimeout;

			ForwardCallToUri = new ByteArrayPart(localUri);

			usedCSeqs = new UsedCSeq[128];

			//dialogs = new Dictionary<int, Dialogs>();
			dialogs1 = new Dictionary<int, Dialog>();
			dialogs2 = new Dictionary<int, Dialog>();
		}

		public void SetServerEndPoint(IPAddress address, int port)
		{
			lock (sync)
			{
				ServerEndPoint = new IPEndPoint(address, port);
			}
		}

		public void SetServerEndPoint(Transports transport, IPAddress address, int port)
		{
			lock (sync)
			{
				Transport = transport;
				ServerEndPoint = new IPEndPoint(address, port);
			}
		}

		public ConnectionAddresses ConnectionAddresses
		{
			get
			{
				lock (sync)
				{
					return new ConnectionAddresses(Transport, LocalEndPoint,
						ServerEndPoint, SocketServers.ServerAsyncEventArgs.AnyNewConnectionId);
				}
			}
		}

		public States State
		{
			get { lock (sync) return state; }
			set
			{
				lock (sync)
				{
					if (state != value)
					{
						state = value;
						if (state == States.Waiting200)
							errorMessage = "Connected";
						OnChanged();
					}
				}
			}
		}

		public bool IsConnected
		{
			get
			{
				lock (sync)
				{
					return state == States.Connected;
				}
			}
		}

		public string ErrorMessage
		{
			get { lock (sync) return errorMessage; }
			set
			{
				lock (sync)
				{
					errorMessage = value;
					state = States.Error;
					OnChanged();
				}
			}
		}

		#region Variable Auth-Values: Nonce, Opaque, Qop, AuthHeader, NonceCount

		public ByteArrayPart Nonce
		{
			get { lock (sync) return nonce; }
		}

		public ByteArrayPart Opaque
		{
			get { lock (sync) return opaque; }
		}

		public AuthQops Qop
		{
			get { lock (sync) return qop; }
		}

		public HeaderNames AuthHeader
		{
			get { lock (sync) return authHeader; }
			set
			{
				lock (sync)
					authHeader = value;
			}
		}

		public bool UpdateChallenge(Challenge? challenge)
		{
			if (challenge.HasValue)
				return UpdateChallenge(challenge.Value);

			return false;
		}

		public bool UpdateChallenge(Challenge challenge)
		{
			return UpdateChallenge(challenge.Nonce, challenge.Opaque, challenge.Qop);
		}

		public bool UpdateChallenge(ByteArrayPart nonce, ByteArrayPart opaque, ByteArrayPart qop)
		{
			lock (sync)
			{
				this.nonce = nonce.DeepCopy();
				this.opaque = opaque.DeepCopy();
				this.qop = qop.IsValid ? AuthQops.Auth : AuthQops.None;

				return true;
			}
		}

		public int GetNextNonceCount()
		{
			lock (sync)
				return ++nonceCount;
		}

		#endregion

		public int GetCSeq(Methods method)
		{
			lock (sync)
			{
				if (method == Methods.Invitem || method == Methods.Byem || method == Methods.Ackm)
					throw new ArgumentOutOfRangeException();

				return ++cseq;
			}
		}

		public int GetCSeq(Methods method, ByteArrayPart callId, int originalCSeq)
		{
			lock (sync)
			{
				if (method != Methods.Invitem && method != Methods.Byem && method != Methods.Ackm)
					return ++cseq;

				int hash = UsedCSeq.CalculateHash(callId, originalCSeq);

				if (method != Methods.Invitem)
				{
					int start = usedCSeqsIndex;

					for (int i = start; i >= 0; i--)
						if (usedCSeqs[i].Hash == hash)
							return usedCSeqs[i].CSeq;

					for (int i = usedCSeqs.Length - 1; i > start; i--)
						if (usedCSeqs[i].Hash == hash)
							return usedCSeqs[i].CSeq;
				}

				int newCSeq = ++inviteCseq;

				usedCSeqsIndex = (usedCSeqsIndex + 1) % usedCSeqs.Length;
				usedCSeqs[usedCSeqsIndex] = new UsedCSeq(hash, newCSeq);

				return newCSeq;
			}
		}

		#region Dialogs

		public void AddDialog1(int tag, Dialog dialog)
		{
			lock (sync)
			{
				dialogs1.Remove(tag);
				dialogs1.Add(tag, dialog);
			}
		}

		public void AddDialog2(int tag, Dialog dialog)
		{
			lock (sync)
			{
				dialogs2.Remove(tag);
				dialogs2.Add(tag, dialog);
			}
		}

		public void RemoveDialog1(int tag)
		{
			lock (sync)
			{
				dialogs1.Remove(tag);
			}
		}

		public void RemoveDialog2(int tag)
		{
			lock (sync)
			{
				dialogs2.Remove(tag);
			}
		}

		public Dialog GetDialog1(int tag)
		{
			lock (sync)
			{
				Dialog dialog;
				if (dialogs1.TryGetValue(tag, out dialog))
					return dialog;
				return null;
			}
		}

		public Dialog GetDialog2(int tag)
		{
			lock (sync)
			{
				Dialog dialog;
				if (dialogs2.TryGetValue(tag, out dialog))
					return dialog;
				return null;
			}
		}

		#endregion

		private void OnChanged()
		{
			var changed = Changed;
			if (changed != null)
				changed(this);
		}
	}
}

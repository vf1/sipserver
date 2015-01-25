using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Linq;
using Sip.Message;
using Base.Message;
using Sip.Server;
using Sip.Server.Accounts;
using Sip.Server.Users;
using Microsoft.Win32.Ssp;
using ThreadSafe = System.Collections.Generic.ThreadSafe;
using Server.Authorization;

namespace Server.Authorization.Sip
{
	using AgentsState = AuthorizationAgentsState<SipMessageWriter>;
	using ShedulerState = AuthorizationShedulerState<SipMessageReader>;
	using IAgent = IAuthorizationAgent<SipMessageReader, SipMessageWriter, AuthSchemes>;
	using ISheduler = IAuthorizationSheduler<SipMessageReader, SipMessageWriter, AuthSchemes>;

	/// <summary>
	/// Представление [MS-SIPAE]: Session Initiation Protocol (SIP) Authentication Extensions NTLM, Kerberos менеджера аутентификации запросов.
	/// </summary>
	class SipMicrosoftAuthentication
		: IDisposable
		, IAuthorizationAgent<SipMessageReader, SipMessageWriter, AuthSchemes>
	{
		#region struct MaxTokenSizes {...}

		struct MaxTokenSizes
		{
			public MaxTokenSizes(int ntlm, int kerberos)
			{
				Ntlm = ntlm;
				Kerberos = kerberos;
			}

			public readonly int Ntlm;
			public readonly int Kerberos;
		}

		#endregion

		#region enum ErrorCodes {...}

		public enum ErrorCodes
		{
			Ok,
			EpidNotFound,
			QopNotSupported,
			UnexpectedOpaque,
			VersionNotSupported,
			NoGssapiData,
			SecurityViolation,
			Continue,
			NoResponse,
			InvalidSignature,
			CrandRequired,
			CnumRequired,
			ResponseRequired,
			QueryContextAttributesForSizesFailed,
			QueryContextAttributesForUsernameFailed,
			UsernameNotMatch,
			ResponseHasInvalidHexEncoding,
			NotAuthenticated,
			NotAuthorized,
		}

		#endregion

		private static readonly ByteArrayPart auth;
		private static readonly ByteArrayPart realm;
		private static readonly ByteArrayPart[] errors;
		private static readonly MaxTokenSizes maxTokenSize;
		private static readonly Func<int, SecurityAssociation, bool> IsSecurityAssociationExpired1;
		private static readonly Action<int, SecurityAssociation> SecurityAssociationRemoved1;
		private static readonly Func<int, AuthSchemes, ByteArrayPart, SecurityAssociation> SecurityAssociationFactory1;
		private static readonly Func<string, SecurityAssociation, bool> IsSecurityAssociationExpired2;
		private static readonly Action<string, SecurityAssociation> SecurityAssociationRemoved2;

		private static readonly ThreadSafe.Dictionary<string, SecurityAssociation> authorizedAssociations;
		//private static ByteArrayPart targetname;
		private static int opaqueCount;

		private readonly IUserz userz;
		private readonly IAccounts accounts;

		static SipMicrosoftAuthentication()
		{
			auth = new ByteArrayPart(@"auth");
			realm = new ByteArrayPart(@"OfficeSIP Server");
			errors = CreateErrorMessages();
			maxTokenSize = GetMaxTokenSizes();
			opaqueCount = Environment.TickCount * 17;

			IsSecurityAssociationExpired1 = (opaque, sa) => { return sa.IsExpired; };
			SecurityAssociationRemoved1 = (opaque, sa) => { sa.Dispose(); };
			SecurityAssociationFactory1 = (opaque, scheme, targetname) => { return new SecurityAssociation(opaque, scheme, targetname); };
			IsSecurityAssociationExpired2 = (epid, sa) => { return sa.IsExpired; };
			SecurityAssociationRemoved2 = (epid, sa) => { sa.Dispose(); };

			authorizedAssociations = new ThreadSafe.Dictionary<string, SecurityAssociation>(new Dictionary<string, SecurityAssociation>());
		}

		//public static void Initialize(string targetname1)
		//{
		//    targetname = new ByteArrayPart(targetname1);
		//}

		private readonly ThreadSafe.Dictionary<int, SecurityAssociation> connectingAssociations;
		private readonly Timer timer;
		private readonly SafeCredHandle credHandle;
		private readonly AuthSchemes scheme;

		public SipMicrosoftAuthentication(AuthSchemes scheme, IAccounts accounts, IUserz userz)
		{
			this.scheme = scheme;
			this.accounts = accounts;
			this.userz = userz;

			this.connectingAssociations = new ThreadSafe.Dictionary<int, SecurityAssociation>(new Dictionary<int, SecurityAssociation>());

			this.timer = new Timer(RemoveExpiredSecurityAssociationsTimer, null, 0, 60 * 1000);
			this.credHandle = Sspi.SafeAcquireCredentialsHandle(scheme.ToString(), CredentialUse.SECPKG_CRED_BOTH);
		}

		public void Dispose()
		{
			credHandle.Dispose();
			timer.Dispose();
		}

		public static ByteArrayPart GetError(ErrorCodes error)
		{
			return errors[(int)error];
		}

		private static ByteArrayPart[] CreateErrorMessages()
		{
			var errors = new ByteArrayPart[Enum.GetValues(typeof(ErrorCodes)).Length];

			errors[(int)ErrorCodes.Ok] = new ByteArrayPart(@"No error");
			errors[(int)ErrorCodes.EpidNotFound] = new ByteArrayPart(@"Epid not found");
			errors[(int)ErrorCodes.QopNotSupported] = new ByteArrayPart(@"Qop not supported");
			errors[(int)ErrorCodes.UnexpectedOpaque] = new ByteArrayPart(@"Unexpected opaque");
			errors[(int)ErrorCodes.VersionNotSupported] = new ByteArrayPart(@"Version not supported");
			errors[(int)ErrorCodes.NoGssapiData] = new ByteArrayPart(@"No gssapi data");
			errors[(int)ErrorCodes.SecurityViolation] = new ByteArrayPart(@"Security violation");
			errors[(int)ErrorCodes.Continue] = new ByteArrayPart(@"Continue");
			errors[(int)ErrorCodes.NoResponse] = new ByteArrayPart(@"No MS auth header");
			errors[(int)ErrorCodes.InvalidSignature] = new ByteArrayPart(@"Invalid Signature");
			errors[(int)ErrorCodes.CrandRequired] = new ByteArrayPart(@"Crand Required");
			errors[(int)ErrorCodes.CnumRequired] = new ByteArrayPart(@"Cnum Required");
			errors[(int)ErrorCodes.ResponseRequired] = new ByteArrayPart(@"Response Required");
			errors[(int)ErrorCodes.QueryContextAttributesForSizesFailed] = new ByteArrayPart(@"QueryContextAttributes for sizes failed");
			errors[(int)ErrorCodes.QueryContextAttributesForUsernameFailed] = new ByteArrayPart(@"QueryContextAttributes for username failed");
			errors[(int)ErrorCodes.UsernameNotMatch] = new ByteArrayPart(@"From username not match to security username");
			errors[(int)ErrorCodes.ResponseHasInvalidHexEncoding] = new ByteArrayPart(@"Response has invalid hex encoding");
			errors[(int)ErrorCodes.NotAuthenticated] = new ByteArrayPart(@"Authentication not completed, signature unexpected");
			errors[(int)ErrorCodes.NotAuthorized] = new ByteArrayPart(@"User pass authentication, but user outside authorized group");

			foreach (var error in errors)
				if (error.Bytes == null)
					throw new InvalidProgramException(@"Not all errors defined");

			return errors;
		}

		private static MaxTokenSizes GetMaxTokenSizes()
		{
			int count;
			SafeContextBufferHandle secPkgInfos;
			if (Sspi.EnumerateSecurityPackages(out count, out secPkgInfos) != SecurityStatus.SEC_E_OK)
				throw new Win32Exception("Failed to EnumerateSecurityPackages");

			int ntlm = 0, kerberos = 0;

			for (int i = 0; i < count; i++)
			{
				var item = secPkgInfos.GetItem<SecPkgInfo>(i);

				if (string.Compare(item.GetName(), @"NTLM", true) == 0)
					ntlm = item.cbMaxToken;
				if (string.Compare(item.GetName(), @"Kerberos", true) == 0)
					kerberos = item.cbMaxToken;
			}

			if (ntlm == 0)
				throw new Exception(@"Failed to retrive cbMaxToken for NTLM");
			if (kerberos == 0)
				throw new Exception(@"Failed to retrive cbMaxToken for Kerberos");

			return new MaxTokenSizes(ntlm, kerberos);
		}

		#region class SecurityAssociation {...}

		protected class SecurityAssociation : IDisposable
		{
			private static readonly int ExpirationHours = 8;
			private static readonly int IdleMinutes = 15;

			[ThreadStatic]
			private static SecBufferDescEx secBufferDesc1;
			[ThreadStatic]
			private static SecBufferDescEx secBufferDesc2;
			[ThreadStatic]
			private static byte[] bytes1;
			[ThreadStatic]
			private static byte[] bytes2;

			private readonly AuthSchemes scheme;
			private readonly ByteArrayPart targetname;

			private DateTime expireTime;
			private DateTime idleTime;
			private bool idleTimeUpdatedByExpires;

			private string userName;
			private int outSnum;
			private int inCnum;

			private SafeCtxtHandle contextHandle;
			private bool isAuthenticationComplete;
			private int maxSignatureSize;

			public readonly int Opaque;

			public SecurityAssociation(int opaque, AuthSchemes scheme, ByteArrayPart targetname)//, SafeCredHandle credentials)
			{
				this.scheme = scheme;
				this.targetname = targetname.DeepCopy();
				this.Opaque = opaque;
				this.contextHandle = new SafeCtxtHandle();

				this.expireTime = DateTime.MaxValue;
				this.idleTime = DateTime.MaxValue;
				this.idleTimeUpdatedByExpires = false;

				this.outSnum = 0;
				this.inCnum = 0;
				//_SlidingWindow.SetAll(false);
			}

			private static void InitializeThreadStaticVars()
			{
				secBufferDesc1 = new SecBufferDescEx(new[] { new SecBufferEx(), new SecBufferEx(), });
				secBufferDesc2 = new SecBufferDescEx(new[] { new SecBufferEx(), new SecBufferEx(), });
				bytes1 = new byte[Math.Max(Math.Max(maxTokenSize.Kerberos, maxTokenSize.Ntlm), 1024)];
				bytes2 = new byte[256];
			}

			public void Dispose()
			{
				contextHandle.Dispose();
			}

			public bool IsExpired
			{
				get
				{
					var now = DateTime.UtcNow;
					bool result = idleTime < now || expireTime < now;

					//if (result)
					//{
					//    SIPServer.Tracer.Info("IdleTime: " + idleTime.ToString());
					//    SIPServer.Tracer.Info("ExpireTime: " + expireTime.ToString());
					//    SIPServer.Tracer.Info("Now: " + now.ToString());
					//}

					return result;
				}
			}

			public ErrorCodes Authentication(SafeCredHandle credHandle, byte[] inToken, out ArraySegment<byte> outToken)
			{
				idleTime = DateTime.UtcNow.AddMinutes(IdleMinutes);

				InitializeThreadStaticVars();

				secBufferDesc1.Buffers[0].SetBuffer(BufferType.SECBUFFER_TOKEN, inToken);
				secBufferDesc1.Buffers[1].SetBufferEmpty();

				secBufferDesc2.Buffers[0].SetBuffer(BufferType.SECBUFFER_TOKEN, bytes1);
				secBufferDesc2.Buffers[1].SetBufferEmpty();

				var newHandle = (contextHandle.IsInvalid) ? new SafeCtxtHandle() : contextHandle;

				var result = Sspi.SafeAcceptSecurityContext(
					ref credHandle,
					ref contextHandle,
					ref secBufferDesc1,
					(int)(ContextReq.ASC_REQ_INTEGRITY | ContextReq.ASC_REQ_IDENTIFY |
						((scheme == AuthSchemes.Ntlm) ? ContextReq.ASC_REQ_DATAGRAM : ContextReq.ASC_REQ_MUTUAL_AUTH)),
					TargetDataRep.SECURITY_NETWORK_DREP,
					ref newHandle,
					ref secBufferDesc2);

				Tracer.WriteInformation("Auth: " + result.ToString());

				if (contextHandle.IsInvalid && newHandle.IsInvalid == false)
					contextHandle = newHandle;

				if (result == SecurityStatus.SEC_E_OK)
				{
					outToken = new ArraySegment<byte>();
					isAuthenticationComplete = true;

					SecPkgContext_Sizes sizes;
					if (Sspi.Failed(Sspi.SafeQueryContextAttributes(ref contextHandle, out sizes)))
						return ErrorCodes.QueryContextAttributesForSizesFailed;
					maxSignatureSize = sizes.cbMaxSignature;

					if (Sspi.Failed(Sspi.SafeQueryContextAttributes(ref contextHandle, out userName)))
						return ErrorCodes.QueryContextAttributesForUsernameFailed;

					int slash = userName.IndexOf('\\');
					if (slash >= 0)
						userName = userName.Substring(slash + 1);

					Tracer.WriteInformation("Username: " + userName);

					expireTime = DateTime.UtcNow.AddHours(ExpirationHours);

					return ErrorCodes.Ok;
				}
				else if (result == SecurityStatus.SEC_I_CONTINUE_NEEDED)
				{
					outToken = new ArraySegment<byte>(bytes1, 0, secBufferDesc2.Buffers[0].Size);
					return ErrorCodes.Continue;
				}
				else
				{
					outToken = new ArraySegment<byte>();
					return ErrorCodes.SecurityViolation;
				}
			}

			public bool IsAuthenticationComplete
			{
				get { return isAuthenticationComplete; }
			}

			public string UserName
			{
				get { return userName; }
			}

			#region static class SignatureBuffer {...}

			static class SignatureBuffer
			{
				private static readonly byte[] sip_ = Encoding.UTF8.GetBytes(@"sip/");

				public static int Generate(AuthSchemes scheme, int srand, int snum, ByteArrayPart targetname, SipMessageWriter writer, ref byte[] bytes)
				{
					int length = 0;

					Write(scheme.ToUtf8Bytes(), ref bytes, ref length);
					WriteAsHex8(srand, ref bytes, ref length);
					Write(snum, ref bytes, ref length);
					Write(SipMicrosoftAuthentication.realm, ref bytes, ref length);

					if (scheme == AuthSchemes.Kerberos)
						Write(sip_, targetname, ref bytes, ref length);
					else
						Write(targetname, ref bytes, ref length);

					Write(writer.CallId, ref bytes, ref length);
					Write(writer.CSeq, ref bytes, ref length);
					Write(writer.Method.ToByteArrayPart(), ref bytes, ref length);
					Write(writer.FromAddrspec, ref bytes, ref length);
					Write(writer.FromTag, ref bytes, ref length);
					Write(writer.ToAddrspec, ref bytes, ref length);
					Write(writer.ToTag, ref bytes, ref length);
					// TODO: sip P-Asserted-Identity
					WriteEmpty(ref bytes, ref length);
					// TODO: tel P-Asserted-Identity
					WriteEmpty(ref bytes, ref length);

					if (writer.Expires != int.MinValue)
						Write(writer.Expires, ref bytes, ref length);
					else
						WriteEmpty(ref bytes, ref length);

					if (writer.IsResponse)
						Write(writer.StatusCode, ref bytes, ref length);

					//SIPServer.Tracer.Info("SignatureBuffer:" + Encoding.UTF8.GetString(bytes, 0, length));

					return length;
				}

				public static int Generate(AuthSchemes scheme, int srand, int snum, ByteArrayPart targetname, SipMessageReader reader, ref byte[] bytes)
				{
					int length = 0;

					Write(scheme.ToUtf8Bytes(), ref bytes, ref length);
					WriteAsHex8(srand, ref bytes, ref length);
					Write(snum, ref bytes, ref length);
					Write(SipMicrosoftAuthentication.realm, ref bytes, ref length);

					if (scheme == AuthSchemes.Kerberos)
						Write(sip_, targetname, ref bytes, ref length);
					else
						Write(targetname, ref bytes, ref length);

					Write(reader.CallId, ref bytes, ref length);
					Write(reader.CSeq.Value, ref bytes, ref length);
					Write(reader.Method.ToByteArrayPart(), ref bytes, ref length);
					Write(reader.From.AddrSpec.Value, ref bytes, ref length);
					Write(reader.From.Tag, ref bytes, ref length);
					Write(reader.To.AddrSpec.Value, ref bytes, ref length);
					Write(reader.To.Tag, ref bytes, ref length);
					// TODO: sip P-Asserted-Identity
					WriteEmpty(ref bytes, ref length);
					// TODO: tel P-Asserted-Identity
					WriteEmpty(ref bytes, ref length);

					if (reader.Expires != int.MinValue)
						Write(reader.Expires, ref bytes, ref length);
					else
						WriteEmpty(ref bytes, ref length);

					if (reader.IsResponse)
						Write(reader.StatusCode.Value, ref bytes, ref length);

					return length;
				}

				public static void WriteEmpty(ref byte[] bytes, ref int length)
				{
					ValidateCapacity(2, ref bytes, length);
					bytes[length++] = 0x3c;
					bytes[length++] = 0x3e;
				}

				public static void Write(ByteArrayPart part, ref byte[] bytes, ref int length)
				{
					ValidateCapacity(part.Length + 2, ref bytes, length);

					bytes[length++] = 0x3c;

					if (part.IsValid)
					{
						Buffer.BlockCopy(part.Bytes, part.Offset, bytes, length, part.Length);
						length += part.Length;
					}

					bytes[length++] = 0x3e;
				}

				public static void Write(byte[] value, ref byte[] bytes, ref int length)
				{
					ValidateCapacity(value.Length + 2, ref bytes, length);

					bytes[length++] = 0x3c;

					Buffer.BlockCopy(value, 0, bytes, length, value.Length);
					length += value.Length;

					bytes[length++] = 0x3e;
				}

				public static void Write(byte[] value1, ByteArrayPart value2, ref byte[] bytes, ref int length)
				{
					ValidateCapacity(value1.Length + value2.Length + 2, ref bytes, length);

					bytes[length++] = 0x3c;

					Buffer.BlockCopy(value1, 0, bytes, length, value1.Length);
					length += value1.Length;

					if (value2.IsValid)
					{
						Buffer.BlockCopy(value2.Bytes, value2.Offset, bytes, length, value2.Length);
						length += value2.Length;
					}

					bytes[length++] = 0x3e;
				}

				public static void Write(int value, ref byte[] bytes, ref int length)
				{
					ValidateCapacity(13, ref bytes, length);

					bytes[length++] = 0x3c;

					bool print = false;

					for (int denominator = 1000000000; denominator >= 10; denominator /= 10)
					{
						byte digit = (byte)(value / denominator);

						if (print = print || digit > 0)
							bytes[length++] = (byte)(0x30 + digit);

						value %= denominator;
					}

					bytes[length++] = (byte)(0x30 + value);
					bytes[length++] = 0x3e;
				}

				public static void WriteAsHex8(int value, ref byte[] bytes, ref int length)
				{
					ValidateCapacity(10, ref bytes, length);

					bytes[length++] = 0x3c;

					length += 8;

					for (int i = 1; i < 9; i++, value >>= 4)
						bytes[length - i] = HexEncoding.HexToLowerAsciiCode[value & 0x0f];

					bytes[length++] = 0x3e;
				}

				private static void ValidateCapacity(int extra, ref byte[] bytes, int length)
				{
					if (bytes.Length - length < extra)
					{
						var oldBytes = bytes;
						bytes = new byte[oldBytes.Length + 512];
						Buffer.BlockCopy(oldBytes, 0, bytes, 0, length);
					}
				}
			}

			#endregion

			public void SignMessage(SipMessageWriter writer)
			{
				outSnum++;
				int srand2 = Environment.TickCount;

				InitializeThreadStaticVars();

				int length = SignatureBuffer.Generate(scheme, srand2, outSnum, targetname, writer, ref bytes1);

				if (bytes2.Length < maxSignatureSize)
					bytes2 = new byte[maxSignatureSize];

				secBufferDesc1.Buffers[0].SetBuffer(BufferType.SECBUFFER_DATA, bytes1, 0, length);
				secBufferDesc1.Buffers[1].SetBuffer(BufferType.SECBUFFER_TOKEN, bytes2);

				var result = Sspi.SafeMakeSignature(contextHandle, ref secBufferDesc1, 100);

				Tracer.WriteInformation("Signature: " + result.ToString());

				if (Sspi.Succeeded(result))
				{
					writer.WriteAuthenticationInfo(true, scheme, targetname, realm, Opaque, outSnum, srand2,
						new ArraySegment<byte>(bytes2, 0, secBufferDesc1.Buffers[1].Size));

					if (writer.IsResponse && writer.StatusCode >= 200 && writer.StatusCode <= 299)
					{
						if (writer.Method == Methods.Registerm && writer.Expires != int.MinValue)
						{
							idleTime = DateTime.UtcNow.AddSeconds(writer.Expires + 30);
							idleTimeUpdatedByExpires = true;
						}
					}
					else
					{
						// TODO: INVITE Session-Expires
					}
				}
			}


			public bool VerifySignature(SipMessageReader reader, Credentials credentials)
			{
				bool result = false;

				InitializeThreadStaticVars();

				int length = SignatureBuffer.Generate(scheme, credentials.Crand, credentials.Cnum, targetname, reader, ref bytes1);

				secBufferDesc1.Buffers[0].SetBuffer(BufferType.SECBUFFER_DATA, bytes1, 0, length);

				if (bytes2.Length < credentials.Response.Length / 2)
					bytes2 = new byte[credentials.Response.Length];

				if (HexEncoding.TryParseHex(credentials.Response.ToArraySegment(), bytes2) >= 0)
				{
					secBufferDesc1.Buffers[1].SetBuffer(BufferType.SECBUFFER_TOKEN, bytes2, 0, credentials.Response.Length / 2);

					var result2 = Sspi.SafeVerifySignature(contextHandle, ref secBufferDesc1, 100);

					Tracer.WriteInformation("VerifySignature: " + result2.ToString());

					result = Sspi.Succeeded(result2);
				}

				{
					//if (auth.Cnum > _LastCnum + SlidingWindowSize || auth.Cnum < _LastCnum - SlidingWindowSize || auth.Cnum == 0)
					//    // cnum в не скользящего окна
					//    throw new AuthenticationManager.MessageDiscardException();

					//// индекс верхней границы скользящего окна
					//int index = auth.Cnum >= SlidingWindowSize ? (auth.Cnum - SlidingWindowSize) % SlidingWindowSize : auth.Cnum - 1;
					//if (auth.Cnum > _LastCnum)
					//{
					//    // индекс нижней границы скользящего окна
					//    int lindex = _LastCnum >= SlidingWindowSize ? (_LastCnum - SlidingWindowSize + 1) % SlidingWindowSize : _LastCnum;

					//    // смещение скользящего окна
					//    if (lindex > index)
					//    {
					//        while (lindex < SlidingWindowSize)
					//            _SlidingWindow[lindex++] = false;
					//        lindex = 0;
					//    }

					//    while (lindex < index)
					//        _SlidingWindow[lindex++] = false;

					//    _LastCnum = auth.Cnum;
					//}
					//else
					//{
					//    if (_SlidingWindow[index] == true)
					//        // дублирование cnum
					//        throw new AuthenticationManager.MessageDiscardException();
					//}

					//_SlidingWindow[index] = true;

					// удаление обработанного поля заголовка
					//message.Header.Headers[message.Header.FindHeaderIndex(auth_name, auth_index)].IsRemoved = true;


					//message.Verified = true;
				}

				if (idleTimeUpdatedByExpires == false)
					idleTime = DateTime.UtcNow.AddMinutes(IdleMinutes);

				return result;
			}

			#region Old Verify Signature

			/// <summary>
			/// Размер скользящего окна.
			/// </summary>
			//static protected readonly int SlidingWindowSize = 256;
			/// <summary>
			/// Скользящее окно.
			/// </summary>
			//protected BitArray _SlidingWindow = new BitArray(SlidingWindowSize);
			/// <summary>
			/// Проверка подписи сообщения.
			/// </summary>
			/// <remarks>
			/// [MS-SIPAE] 3.3.5.3 Processing Authorized Messages from the SIP Client
			/// [MS-SIPAE] 3.3.2 Timers
			/// </remarks>
			//    public void Verifying(SIPMessage message, string realm)
			//    {
			//        // TODO: выборка первого поля Authorization или Proxy-Authorization

			//        Credentials auth = new Credentials();
			//        HeaderNames auth_name;
			//        int auth_index = 0;

			//        if (MSAuthenticationManager.ParametersCheck(message.Header.Authorization[0], HeaderNames.Authorization, _Scheme, realm) == true)
			//        {
			//            // Authorization
			//            auth = message.Header.Authorization[0];
			//            auth_name = HeaderNames.Authorization;
			//        }
			//        else
			//        {
			//            if (MSAuthenticationManager.ParametersCheck(message.Header.ProxyAuthorization[0], HeaderNames.ProxyAuthorization, _Scheme, realm) == true)
			//            {
			//                // Proxy-Authorization
			//                auth = message.Header.ProxyAuthorization[0];
			//                auth_name = HeaderNames.ProxyAuthorization;
			//                message.AuthenticationProxy = true;
			//            }
			//            else
			//            {
			//                auth.AuthScheme = AuthSchemes.None;
			//                auth_name = HeaderNames.None;
			//            }
			//        }

			//        if (auth.AuthScheme != AuthSchemes.None)
			//        {
			//            // проверка соответствия qop
			//            if ((auth.Opaque.IsValid == true) &&
			//                //(auth.Opaque.ToString() == _Opaque) &&
			//                (auth.Opaque.ToString() == Opaque.ToString("x8")) &&
			//                (auth.MessageQop.ToString() == @"auth") &&
			//                (auth.Crand != int.MinValue) &&
			//                (auth.Cnum != int.MinValue) &&
			//                (auth.Response.IsValid == true))
			//            {
			//                var buffer =
			//                    @"<" + _Scheme.ToString1() + @">" +
			//                    @"<" + auth.Crand.ToString(@"x8") + @">" +
			//                    @"<" + auth.Cnum.ToString(@"d") + @">" +
			//                    @"<" + MSAuthenticationManager.Realm + @">" +
			//                    @"<" + (_Scheme == AuthSchemes.Kerberos ? @"sip/" : @"") + _Targetname + @">" +
			//                    @"<" + message.Header.CallId.ToString() + @">" +
			//                    @"<" + message.Header.CSeq.Value.ToString(@"d") + @">" +
			//                    @"<" + message.Header.CSeq.Method.ToString1() + @">" +
			//                    @"<" + message.Header.From.AddrSpec1.Value.ToString() + @">" +
			//                    @"<" + message.Header.From.Tag.ToString() + @">" +
			//                    @"<" + message.Header.To.AddrSpec1.Value.ToString() + @">" +
			//                    @"<" + message.Header.To.TagString() + @">" +
			//                    // TODO: sip P-Asserted-Identity
			//                    @"<>" +
			//                    // TODO: tel P-Asserted-Identity
			//                    @"<>" +
			//                    @"<" + (message.Header.Expires != int.MinValue ? message.Header.Expires.ToString(@"d") : @"") + @">" +
			//                    (message.IsResponse == true ? @"<" + message.Header.StatusCode.ToString() + @">" : @"");

			//                try
			//                {
			//                    if (_SSPI.Verify(Encoding.UTF8.GetBytes(buffer), Utility.FromLHEXString(auth.Response.ToString()), 100) == true)
			//                    {
			//                        if (auth.Cnum > _LastCnum + SlidingWindowSize || auth.Cnum < _LastCnum - SlidingWindowSize || auth.Cnum == 0)
			//                            // cnum в не скользящего окна
			//                            throw new AuthenticationManager.MessageDiscardException();

			//                        // индекс верхней границы скользящего окна
			//                        int index = auth.Cnum >= SlidingWindowSize ? (auth.Cnum - SlidingWindowSize) % SlidingWindowSize : auth.Cnum - 1;
			//                        if (auth.Cnum > _LastCnum)
			//                        {
			//                            // индекс нижней границы скользящего окна
			//                            int lindex = _LastCnum >= SlidingWindowSize ? (_LastCnum - SlidingWindowSize + 1) % SlidingWindowSize : _LastCnum;

			//                            // смещение скользящего окна
			//                            if (lindex > index)
			//                            {
			//                                while (lindex < SlidingWindowSize)
			//                                    _SlidingWindow[lindex++] = false;
			//                                lindex = 0;
			//                            }

			//                            while (lindex < index)
			//                                _SlidingWindow[lindex++] = false;

			//                            _LastCnum = auth.Cnum;
			//                        }
			//                        else
			//                        {
			//                            if (_SlidingWindow[index] == true)
			//                                // дублирование cnum
			//                                throw new AuthenticationManager.MessageDiscardException();
			//                        }

			//                        _SlidingWindow[index] = true;

			//                        // удаление обработанного поля заголовка
			//                        message.Header.Headers[message.Header.FindHeaderIndex(auth_name, auth_index)].IsRemoved = true;


			//                        message.Verified = true;
			//                    }
			//                }
			//                catch (Win32Exception)
			//                {
			//                }
			//            }
			//        }

			//        if (_IdleExpires == false)
			//            _IdleDateTime = DateTime.UtcNow.AddMinutes(IdleMinutes);
			//    }

			#endregion

		}

		#endregion

		protected void RemoveExpiredSecurityAssociationsTimer(Object state)
		{
			connectingAssociations.Remove(IsSecurityAssociationExpired1, SecurityAssociationRemoved1, 64);
			authorizedAssociations.Remove(IsSecurityAssociationExpired2, SecurityAssociationRemoved2, 64);
		}

		public static void SignMessage(SipMessageWriter writer)
		{
			Tracer.WriteInformation("SingMessage epid: " + writer.Epid.ToString());

			if (writer.Epid.IsValid)
			{
				SecurityAssociation sa;
				if (authorizedAssociations.TryGetValue(writer.Epid.ToString(), out sa))
					if (sa.IsAuthenticationComplete)
						sa.SignMessage(writer);
			}
		}

		public ErrorCodes Authorize(SipMessageReader reader, AuthSchemes scheme, out ArraySegment<byte> token, out int opaque, out bool proxy)
		{
			token = new ArraySegment<byte>();

			//var credentials = reader.GetCredentialsByTargetname(scheme, targetname, out proxy);
			IAccount account;
			var credentials = FindCredentials(reader, out account, out proxy);

			if (HexEncoding.TryParseHex8(credentials.Opaque, out opaque) == false)
				opaque = Interlocked.Increment(ref opaqueCount);

			if (credentials.AuthScheme == AuthSchemes.None)
				return ErrorCodes.NoResponse;

			if (credentials.MessageQop.Equals(auth) == false)
				return ErrorCodes.QopNotSupported;

			var epid = reader.From.Epid;
			if (epid.IsInvalid)
				return ErrorCodes.EpidNotFound;

			if (credentials.HasGssapiData)
			{
				if (credentials.Version != 3)
					return ErrorCodes.VersionNotSupported;

				var sa = connectingAssociations.GetOrAdd(opaque, scheme, GetDomain(credentials.Targetname), SecurityAssociationFactory1);

				var result = sa.Authentication(credHandle,
					Convert.FromBase64String(credentials.GssapiData.IsValid ? credentials.GssapiData.ToString() : String.Empty),
					out token);

				if (result != ErrorCodes.Continue)
				{
					connectingAssociations.Remove(opaque);

					if (result == ErrorCodes.Ok)
					{
						if (sa.UserName.Equals(reader.From.AddrSpec.User.ToString(), StringComparison.OrdinalIgnoreCase) == false)
						{
							result = ErrorCodes.UsernameNotMatch;
						}
						else
						{
							if (IsAuthorized(GetDomain(credentials.Targetname), sa.UserName.ToLower()) == false)
							{
								result = ErrorCodes.NotAuthorized;
							}
							else
							{
								var old = authorizedAssociations.Replace(epid.ToString(), sa);
								if (old != null)
									old.Dispose();
							}
						}
					}

					if (result != ErrorCodes.Ok)
					{
						sa.Dispose();
					}
				}

				return result;
			}
			else
			{
				// verify signature
				//

				SecurityAssociation sa;
				if (authorizedAssociations.TryGetValue(epid.ToString(), out sa) == false)
					return ErrorCodes.NotAuthenticated;

				if (credentials.Crand == int.MinValue)
					return ErrorCodes.CrandRequired;

				if (credentials.Cnum == int.MinValue)
					return ErrorCodes.CnumRequired;

				if (credentials.HasResponse == false)
					return ErrorCodes.ResponseRequired;

				if (credentials.Opaque.IsValid && opaque != sa.Opaque)
					return ErrorCodes.UnexpectedOpaque;

				if (sa.VerifySignature(reader, credentials) == false)
					return ErrorCodes.InvalidSignature;

				if (sa.UserName.Equals(reader.From.AddrSpec.User.ToString(), StringComparison.OrdinalIgnoreCase) == false)
					return ErrorCodes.UsernameNotMatch;

				return ErrorCodes.Ok;
			}
		}

		private Credentials FindCredentials(SipMessageReader reader, out IAccount account, out bool proxy)
		{
			for (int i = 0; i < reader.Count.AuthorizationCount; i++)
			{
				if (reader.Authorization[i].AuthScheme == scheme)
				{
					account = accounts.GetAccount(GetDomain(reader.Authorization[i].Targetname));
					if (account != null)
					{
						proxy = false;
						return reader.Authorization[i];
					}
				}
			}

			for (int i = 0; i < reader.Count.ProxyAuthorizationCount; i++)
			{
				if (reader.ProxyAuthorization[i].AuthScheme == scheme)
				{
					account = accounts.GetAccount(GetDomain(reader.ProxyAuthorization[i].Targetname));
					if (account != null)
					{
						proxy = true;
						return reader.ProxyAuthorization[i];
					}
				}
			}

			account = null;
			proxy = false;
			return new Credentials();
		}

		private ByteArrayPart GetDomain(BeginEndIndex targetname)
		{
			var result = targetname;
			if (scheme == AuthSchemes.Kerberos)
				result.Begin += 4;
			return result;
		}

		private bool IsAuthorized(ByteArrayPart domain, string name)
		{
			var account = accounts.GetAccount(domain);

			if (account != null)
			{
				for (int i = 0; i < userz.Count; i++)
				{
					if (userz[i].GetByName(account.Id, name) != null)
						return true;
				}
			}

			return false;
		}

		#region IAuthorizationAgent

		AgentsState IAgent.IsAuthorized(ISheduler sheduler, ShedulerState state)
		{
			int opaque;
			bool proxy;
			ArraySegment<byte> token;
			var error = Authorize(state.Reader, scheme, out token, out opaque, out proxy);

			AuthorizationError error1;
			switch (error)
			{
				case ErrorCodes.Ok:
					error1 = AuthorizationError.Success;
					break;
				case ErrorCodes.Continue:
					error1 = AuthorizationError.Continue;
					break;
				case ErrorCodes.NoResponse:
					error1 = AuthorizationError.None;
					break;
				default:
					error1 = AuthorizationError.Failed;
					break;
			}

			var response = sheduler.GetCommand(state, scheme, error1);
			var targetname = state.Reader.RequestUri.Hostport.Host;

			if (response.Command == AuthorizationCommands.TryAgain)
			{
				response.Writer.WriteAuthenticateMs(proxy, scheme, targetname, realm, opaque);
				response.Writer.WriteXErrorDetails(GetError(error));
				response.Writer.WriteDate(DateTime.UtcNow);
			}
			else if (response.Command == AuthorizationCommands.Continue)
			{
				response.Writer.WriteAuthenticateMs(proxy, scheme, targetname, realm, opaque, token);
				response.Writer.WriteDate(DateTime.UtcNow);
			}
			else if (response.Command == AuthorizationCommands.Cancel)
			{
				response.Writer.WriteXErrorDetails(GetError(error));
			}

			return new AgentsState(response);
		}

		#endregion
	}
}

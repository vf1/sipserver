using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Base.Message;
using Sip.Server;
using Sip.Server.Accounts;
using Sip.Server.Users;
using SocketServers;
using ThreadSafe = System.Collections.Generic.ThreadSafe;
using SipMethods = Sip.Message.Methods;

namespace Server.Authorization
{
	class DigestAuthentication
		: IDisposable
	{
		#region struct Nonce {...}

		protected struct Nonce
		{
			private const int xorOpaque = 0x62fea12f;

			public Nonce(byte[] bytes, int startIndex)
			{
				Data1 = BitConverter.ToInt32(bytes, startIndex + 0);
				Data2 = BitConverter.ToInt32(bytes, startIndex + 4);
				Data3 = BitConverter.ToInt32(bytes, startIndex + 8);
				Data4 = BitConverter.ToInt32(bytes, startIndex + 12);
			}

			public Nonce(byte[] bytes, int startIndex, int opaque)
			{
				Data1 = BitConverter.ToInt32(bytes, startIndex + 0);
				Data2 = BitConverter.ToInt32(bytes, startIndex + 4);
				Data3 = BitConverter.ToInt32(bytes, startIndex + 8);
				Data4 = Data1 ^ Data2 ^ Data3 ^ xorOpaque ^ opaque;
			}

			public Nonce(int data1, int data2, int data3, int data4)
			{
				Data1 = data1;
				Data2 = data2;
				Data3 = data3;
				Data4 = data4;
			}

			public readonly int Data1;
			public readonly int Data2;
			public readonly int Data3;
			public readonly int Data4;

			public int DecodeOpaque()
			{
				return Data1 ^ Data2 ^ Data3 ^ xorOpaque ^ Data4;
			}

			public bool IsEqualValue(Nonce y)
			{
				return Data1 == y.Data1 && Data2 == y.Data2 && Data3 == y.Data3 && Data4 == y.Data4;
			}

			private static byte[] bytes16 = new byte[16];
			private static RNGCryptoServiceProvider cryptoRandom = new RNGCryptoServiceProvider();

			public static Nonce Generate()
			{
				lock (cryptoRandom)
				{
					cryptoRandom.GetBytes(bytes16);
					return new Nonce(bytes16, 0);
				}
			}

			public static Nonce Generate(int opaque)
			{
				lock (cryptoRandom)
				{
					cryptoRandom.GetBytes(bytes16);
					return new Nonce(bytes16, 0, opaque);
				}
			}

			public static bool TryParse(byte[] hexBytes32, int startIndex, out Nonce nonce)
			{
				int data1 = 0, data2 = 0, data3 = 0, data4 = 0;

				if (HexEncoding.TryParseHex8(hexBytes32, startIndex + 0, out data1) &&
					HexEncoding.TryParseHex8(hexBytes32, startIndex + 8, out data2) &&
					HexEncoding.TryParseHex8(hexBytes32, startIndex + 16, out data3) &&
					HexEncoding.TryParseHex8(hexBytes32, startIndex + 24, out data4))
				{
					nonce = new Nonce(data1, data2, data3, data4);
					return true;
				}

				nonce = new Nonce();
				return false;
			}

			public int HashCode
			{
				get { return Data1 ^ Data2 ^ Data3 ^ Data4; }
			}
		}

		#endregion

		#region struct AuthState {...}

		protected struct AuthState
		{
			public AuthState(int opaque)
			{
				Opaque = opaque;
				Nonce = Nonce.Generate(Opaque);
				LastAccess = Environment.TickCount;
				NonceCount = int.MinValue;
			}

			public Nonce Nonce;
			public int NonceCount;
			public int LastAccess;
			public int Opaque;

			public bool IsNonceCountExpected
			{
				get { return NonceCount > int.MinValue; }
			}
		}

		#endregion

		#region class ResponseCalculator {...}

		protected class ResponseCalculator
			: IDisposable
		{
			private MD5 md5;
			private byte[] bytes32;

			public static readonly byte[] Colon = Encoding.UTF8.GetBytes(@":");
			public static readonly ByteArrayPart AuthInt = new ByteArrayPart(@"auth-int");
			public static readonly ByteArrayPart Auth = new ByteArrayPart(@"auth");

			public ResponseCalculator()
			{
				md5 = MD5.Create();
				bytes32 = new byte[32];
			}

			//public bool IsResponseValid(ByteArrayPart username, ByteArrayPart realm,
			//    bool isMd5SessAlgorithm, ByteArrayPart nonce, ByteArrayPart cnonce,
			//    ByteArrayPart qop, ByteArrayPart digestUri, ByteArrayPart nonceCountBytes,
			//    ByteArrayPart method, ArraySegment<byte> body, ByteArrayPart password)
			//{
			//    byte[] ha1 = CalculateHa1(username, realm, isMd5SessAlgorithm, nonce, cnonce, password);
			//    byte[] ha2 = CalculateHa2(qop, digestUri, method, body);
			//    byte[] response = CalculateResponse(nonce, qop, nonceCountBytes, cnonce, ha1, ha2);

			//    HexEncoding.GetLowerHexChars(response, bytes32); // ?? что если ответ будет в upper case
			//    return cred.Response.Equals(bytes32);
			//}

			public byte[] GetResponseHexChars(ByteArrayPart username, ByteArrayPart realm,
				bool isMd5SessAlgorithm, ByteArrayPart nonce, ByteArrayPart cnonce,
				ByteArrayPart qop, ByteArrayPart digestUri, ByteArrayPart nonceCountBytes,
				ByteArrayPart method, ArraySegment<byte> body, ByteArrayPart password)
			{
				byte[] ha1 = CalculateHa1(username, realm, isMd5SessAlgorithm, nonce, cnonce, password);
				byte[] ha2 = CalculateHa2(qop, digestUri, method, body);
				byte[] response = CalculateResponse(nonce, qop, nonceCountBytes, cnonce, ha1, ha2);

				HexEncoding.GetLowerHexChars(response, bytes32); // ?? что если ответ будет в upper case

				return bytes32;
			}

			public byte[] GetResponseHexChars(ByteArrayPart username, ByteArrayPart realm, bool isMd5SessAlgorithm,
				ByteArrayPart nonce, int cnonce, int nonceCount, ByteArrayPart password,
				ByteArrayPart qop, ByteArrayPart digestUri, ByteArrayPart method, ArraySegment<byte> body)
			{
				var bytes24 = new byte[24];
				var nonceCountBytes = GetDigitChars(bytes24, nonceCount, 15);
				HexEncoding.GetLowerHexChars(cnonce, bytes24, 16);
				var cnonceBytes = new ByteArrayPart() { Bytes = bytes24, Begin = 16, End = 24, };

				byte[] ha1 = CalculateHa1(username, realm, isMd5SessAlgorithm, nonce, cnonceBytes, password);
				byte[] ha2 = CalculateHa2(qop, digestUri, method, body);
				byte[] response = CalculateResponse(nonce, qop, nonceCountBytes, cnonceBytes, ha1, ha2);

				HexEncoding.GetLowerHexChars(response, bytes32);

				return bytes32;
			}

			private ByteArrayPart GetDigitChars(byte[] bytes, int value, int endIndex)
			{
				int count = 0;

				do
				{
					bytes[endIndex - count] = (byte)(0x30 + value % 10);
					value /= 10;
					count++;
				}
				while (value > 0);

				return new ByteArrayPart() { Bytes = bytes, Begin = endIndex - count + 1, End = endIndex + 1, };
			}

			public byte[] CalculateResponse(ByteArrayPart nonce, ByteArrayPart qop,
				ByteArrayPart nonceCountBytes, ByteArrayPart cnonce, byte[] ha1, byte[] ha2)
			{
				md5.Initialize();

				HexEncoding.GetLowerHexChars(ha1, bytes32);
				md5.TransformBlock(bytes32);
				md5.TransformBlock(Colon);
				md5.TransformBlock(nonce);
				md5.TransformBlock(Colon);

				if (qop.IsValid)
				{
					md5.TransformBlock(nonceCountBytes);
					md5.TransformBlock(Colon);
					md5.TransformBlock(cnonce);
					md5.TransformBlock(Colon);
					md5.TransformBlock(qop);
					md5.TransformBlock(Colon);
				}

				HexEncoding.GetLowerHexChars(ha2, bytes32);
				md5.TransformFinalBlock(bytes32);

				return md5.Hash;
			}

			public byte[] CalculateHa2(ByteArrayPart qop, ByteArrayPart digestUri, ByteArrayPart method, ArraySegment<byte> body)
			{
				byte[] bodyHash = null;
				//	if (messageQop.IsValid == true && messageQop.IsEqualValue(AuthInt))
				if (qop.Equals(AuthInt))
					if (body.Array != null && body.Count > 0)
						bodyHash = md5.ComputeHash(body.Array, body.Offset, body.Count);

				md5.Initialize();
				md5.TransformBlock(method);
				md5.TransformBlock(Colon);

				if (bodyHash != null)
				{
					md5.TransformBlock(digestUri);
					md5.TransformBlock(Colon);
					HexEncoding.GetLowerHexChars(bodyHash, bytes32);
					md5.TransformFinalBlock(bytes32);
				}
				else
					md5.TransformFinalBlock(digestUri);

				return md5.Hash;
			}

			public byte[] CalculateHa1(ByteArrayPart username, ByteArrayPart realm,
				bool isMd5SessAlgorithm, ByteArrayPart nonce, ByteArrayPart cnonce, ByteArrayPart password)
			{
				md5.Initialize();

				md5.TransformBlock(username);
				md5.TransformBlock(Colon);
				md5.TransformBlock(realm);
				md5.TransformBlock(Colon);
				md5.TransformFinalBlock(password);

				if (isMd5SessAlgorithm)
				{
					var ha1 = md5.Hash;

					md5.Initialize();
					md5.TransformBlock(ha1);
					md5.TransformBlock(Colon);
					md5.TransformBlock(nonce);
					md5.TransformBlock(Colon);
					md5.TransformFinalBlock(cnonce);
				}

				return md5.Hash;
			}

			void IDisposable.Dispose()
			{
				md5.Clear();
			}
		}

		#endregion

		protected readonly IAccounts accounts;
		protected readonly IUserz userz;
		protected readonly bool isAuthIntEnabled;
		protected Timer timer;
		protected Int32 opaqueCount;
		protected ThreadSafe.Dictionary<int, AuthState> authStates;
		[ThreadStatic]
		protected static ResponseCalculator responseCalculator;

		public const int NonceLifeCheckInterval = 60 * 1000;
		public const int NonceLifeInterval = 60 * 60 * 1000;

		private static readonly byte[][] errors;

		static DigestAuthentication()
		{
			errors = CreateErrorMessages();
		}

		public DigestAuthentication(IAccounts accounts, IUserz userz, bool isAuthIntEnabled)
		{
			this.accounts = accounts;
			this.userz = userz;
			this.isAuthIntEnabled = isAuthIntEnabled;

			authStates = new ThreadSafe.Dictionary<int, AuthState>(new Dictionary<int, AuthState>(16384)); // vf Int32Compa...
			timer = new Timer(RemoveExpiredNonce, null, 0, NonceLifeCheckInterval);
		}

		private static byte[][] CreateErrorMessages()
		{
			var errors = new byte[Enum.GetValues(typeof(ErrorCodes)).Length][];

			errors[(int)ErrorCodes.Ok] = Encoding.UTF8.GetBytes(@"No error");
			errors[(int)ErrorCodes.NonceInvalid] = Encoding.UTF8.GetBytes(@"Nonce invalid");
			errors[(int)ErrorCodes.FailedToParseNonce] = Encoding.UTF8.GetBytes(@"Failed to parse nonce");
			errors[(int)ErrorCodes.AuthStateNotFound] = Encoding.UTF8.GetBytes(@"Auth state not found");
			errors[(int)ErrorCodes.NonceStale] = Encoding.UTF8.GetBytes(@"Nonce stale");
			errors[(int)ErrorCodes.NonceCountExpected] = Encoding.UTF8.GetBytes(@"Nonce count expected");
			errors[(int)ErrorCodes.NonceCountInvalid] = Encoding.UTF8.GetBytes(@"Nonce must increase on each request");
			errors[(int)ErrorCodes.FailedToRetriveUserPassword] = Encoding.UTF8.GetBytes(@"Failed to retrive user password");
			errors[(int)ErrorCodes.NotSupportedAuthAlgorithm] = Encoding.UTF8.GetBytes(@"Not supported auth algorithm");
			errors[(int)ErrorCodes.WrongResponse] = Encoding.UTF8.GetBytes(@"Wrong response");
			errors[(int)ErrorCodes.UsernmaeNotFound] = Encoding.UTF8.GetBytes(@"Username not found");
			errors[(int)ErrorCodes.RealmNotFound] = Encoding.UTF8.GetBytes(@"Realm not found");
			errors[(int)ErrorCodes.NonceNotFound] = Encoding.UTF8.GetBytes(@"Nonce not found");
			errors[(int)ErrorCodes.CnonceNotFound] = Encoding.UTF8.GetBytes(@"Cnonce not found");
			errors[(int)ErrorCodes.DigestUriNotFound] = Encoding.UTF8.GetBytes(@"Digest uri not found");
			errors[(int)ErrorCodes.ResponseNotFound] = Encoding.UTF8.GetBytes(@"Response not found");
			errors[(int)ErrorCodes.InvalidRealm] = Encoding.UTF8.GetBytes(@"Invalid realm");
			errors[(int)ErrorCodes.UsernamesNotMatch] = Encoding.UTF8.GetBytes(@"From username does not match to credentials");
			errors[(int)ErrorCodes.NoResponse] = Encoding.UTF8.GetBytes(@"No auth header");
			errors[(int)ErrorCodes.InvalidDomain] = Encoding.UTF8.GetBytes(@"Domain invalid or not specified");
			errors[(int)ErrorCodes.NotAuthorized] = Encoding.UTF8.GetBytes(@"Username not authorized");

			foreach (var error in errors)
				if (error == null)
					throw new InvalidProgramException(@"Not all errors defined");

			return errors;
		}

		public void Dispose()
		{
			timer.Dispose();
			authStates.Dispose();
		}

		private void RemoveExpiredNonce(Object obj)
		{
			int tickCount = Environment.TickCount;

			authStates.Remove(
				(state) => { return unchecked(tickCount - state.LastAccess) > NonceLifeInterval; });
		}

		//public static byte[] GetResponseHexChars(ByteArrayPart username, ByteArrayPart realm, AuthAlgorithms algorithm,
		//        ByteArrayPart nonce, int cnonce, int nonceCount, ByteArrayPart password,
		//        AuthQops qop, ByteArrayPart digestUri, ByteArrayPart method, ArraySegment<byte> body)
		//{
		//    if (responseCalculator == null)
		//        responseCalculator = new ResponseCalculator();

		//    return responseCalculator.GetResponseHexChars(username, realm, algorithm, nonce,
		//        cnonce, nonceCount, password, qop, digestUri, method, body);
		//}

		public enum ErrorCodes
		{
			Ok,
			NonceInvalid,
			FailedToParseNonce,
			AuthStateNotFound,
			NonceStale,
			NonceCountExpected,
			NonceCountInvalid,
			FailedToRetriveUserPassword,
			NotSupportedAuthAlgorithm,
			WrongResponse,
			UsernmaeNotFound,
			RealmNotFound,
			NonceNotFound,
			CnonceNotFound,
			DigestUriNotFound,
			ResponseNotFound,
			InvalidRealm,
			UsernamesNotMatch,
			NoResponse,
			InvalidDomain,
			NotAuthorized,
		}

		private string GetPassword(int accountId, ByteArrayPart username)
		{
			var username2 = username.ToString();

			for (int i = 0; i < userz.Count; i++)
			{
				if (userz[i].HasPasswords)
				{
					var user = userz[i].GetByName(accountId, username2);

					if (user != null)
						return user.Password;
				}
			}

			return null;
		}

		public bool IsNonceStale(ErrorCodes errorCode)
		{
			return errorCode == ErrorCodes.NonceStale || errorCode == ErrorCodes.AuthStateNotFound;
		}

		public byte[] GetError(ErrorCodes error)
		{
			return errors[(int)error];
		}

		public AuthorizationError GetAuthorizationError(ErrorCodes errorCode)
		{
			if (errorCode == ErrorCodes.Ok)
				return AuthorizationError.Success;

			if (errorCode == ErrorCodes.NoResponse)
				return AuthorizationError.None;

			return AuthorizationError.Failed;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Sip.Message;
using Base.Message;
using Sip.Server;
using Sip.Server.Accounts;
using Sip.Server.Users;
using SocketServers;
using ThreadSafe = System.Collections.Generic.ThreadSafe;

namespace Server.Authorization.Sip
{
	using AgentsState = AuthorizationAgentsState<SipMessageWriter>;
	using ShedulerState = AuthorizationShedulerState<SipMessageReader>;
	using IAgent = IAuthorizationAgent<SipMessageReader, SipMessageWriter, AuthSchemes>;
	using ISheduler = IAuthorizationSheduler<SipMessageReader, SipMessageWriter, AuthSchemes>;

	class SipDigestAuthentication
		: DigestAuthentication
		, IAuthorizationAgent<SipMessageReader, SipMessageWriter, AuthSchemes>
		, IDisposable
	{
		public SipDigestAuthentication(IAccounts accounts, IUserz userz, bool isAuthIntEnabled)
			: base(accounts, userz, isAuthIntEnabled)
		{
		}

		public static byte[] GetResponseHexChars(ByteArrayPart username, ByteArrayPart realm, AuthAlgorithms algorithm,
				ByteArrayPart nonce, int cnonce, int nonceCount, ByteArrayPart password,
				AuthQops qop, ByteArrayPart digestUri, ByteArrayPart method, ArraySegment<byte> body)
		{
			if (responseCalculator == null)
				responseCalculator = new ResponseCalculator();

			return responseCalculator.GetResponseHexChars(username, realm, algorithm == AuthAlgorithms.Md5Sess, nonce,
				cnonce, nonceCount, password, qop.ToByteArrayPart(), digestUri, method, body);
		}

		public ErrorCodes Authorize(SipMessageReader reader, ArraySegment<byte> content, AuthenticationKind kind)
		{
			//if (state.Message.Reader.Count.AuthorizationCount > 0)
			//	return ErrorCodes.NoResponse;

			// User-Agent: X-Lite release 1104o stamp 56125
			// It use old Authorization header from INVITE request to ACK request:
			//  1. Do NOT increment nc
			//  2. Uses INVITE method for response calculation
			// User-Agent: 3CXPhone 5.0.14900.0
			// Look like X-Lite clone -> disable auth for all ACK
			if (reader.CSeq.Method == Methods.Ackm) // && reader.UserAgent.IsXLite)
				return ErrorCodes.Ok;

			IAccount account;
			var credentials = FindCredentials(reader, out account);

			// User-Agent: NCH Software Express Talk 4.15
			// Quotes message-qop value, qop="auth"
			//if (credentials.MessageQop.IsInvalid && reader.UserAgent.IsNch)
			//    credentials.MessageQop = ResponseCalculator.Auth;

			if (credentials.AuthScheme != AuthSchemes.Digest)
				return ErrorCodes.NoResponse;

			if (credentials.AuthAlgorithm == AuthAlgorithms.Other)
				return ErrorCodes.NotSupportedAuthAlgorithm;

			if (credentials.Username.IsInvalid)
				return ErrorCodes.UsernmaeNotFound;

			if (credentials.Username.Equals(reader.From.AddrSpec.User) == false)
				return ErrorCodes.UsernamesNotMatch;

			if (credentials.Realm.IsInvalid)
				return ErrorCodes.RealmNotFound;

			if (credentials.Nonce.IsInvalid)
				return ErrorCodes.NonceNotFound;

			if (credentials.MessageQop.IsValid)
			{
				if (credentials.Cnonce.IsInvalid)
					return ErrorCodes.CnonceNotFound;
			}

			if (credentials.DigestUri.IsInvalid)
				return ErrorCodes.DigestUriNotFound;

			if (credentials.Response.IsInvalid)
				return ErrorCodes.ResponseNotFound;

			//if (credentials.Realm.Equals(realm1) == false)
			//	return ErrorCodes.InvalidRealm;

			if (credentials.Nonce.Length != 32)
				return ErrorCodes.NonceInvalid;

			Nonce nonce;
			if (Nonce.TryParse(credentials.Nonce.Bytes, credentials.Nonce.Begin, out nonce) == false)
				return ErrorCodes.FailedToParseNonce;

			int opaque = nonce.DecodeOpaque();

			AuthState state;
			if (authStates.TryGetValue(opaque, out state) == false)
				return ErrorCodes.AuthStateNotFound;

			if (state.Nonce.IsEqualValue(nonce) == false)
				return ErrorCodes.NonceStale;

			if (state.NonceCount >= credentials.NonceCount && state.IsNonceCountExpected)
				return ErrorCodes.NonceCountExpected;

			var password = GetPassword(account.Id, credentials.Username);
			if (password == null)
				return ErrorCodes.FailedToRetriveUserPassword;

			if (responseCalculator == null)
				responseCalculator = new ResponseCalculator();
			//var responseCalculator = base.GetResponseCalculator();

			if (reader.Method == Methods.Extension || reader.Method == Methods.None) // нужно исправить!!!!!!!!!!!!!!!!!!!
				return ErrorCodes.NotSupportedAuthAlgorithm;

			//if (responseCalculator.IsResponseValid(credentials, reader.Method.ToByteArrayPart(), content, password.ToByteArrayPart()) == false)
			//    return ErrorCodes.WrongResponse;

			var response = responseCalculator.GetResponseHexChars(
				credentials.Username,
				credentials.Realm,
				credentials.AuthAlgorithm == AuthAlgorithms.Md5Sess,
				credentials.Nonce,
				credentials.Cnonce,
				credentials.MessageQop,
				credentials.DigestUri,
				credentials.NonceCountBytes,
				reader.Method.ToByteArrayPart(),
				content,
				password.ToByteArrayPart());

			if (credentials.Response.Equals(response) == false)
				return ErrorCodes.WrongResponse;

			state.NonceCount = credentials.NonceCount;
			state.LastAccess = unchecked(Environment.TickCount - 10000);
			authStates.Replace(state.Opaque, state);

			return ErrorCodes.Ok;
		}

		private Credentials FindCredentials(SipMessageReader reader, out IAccount account)
		{
			for (int i = 0; i < reader.Count.AuthorizationCount; i++)
			{
				if (reader.Authorization[i].AuthScheme == AuthSchemes.Digest)
				{
					account = accounts.GetAccount(reader.Authorization[i].Realm);
					if (account != null)
						return reader.Authorization[i];
				}
			}

			for (int i = 0; i < reader.Count.ProxyAuthorizationCount; i++)
			{
				if (reader.ProxyAuthorization[i].AuthScheme == AuthSchemes.Digest)
				{
					account = accounts.GetAccount(reader.ProxyAuthorization[i].Realm);
					if (account != null)
						return reader.ProxyAuthorization[i];
				}
			}

			account = null;
			return new Credentials();
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

		public void WriteUnauthenticate(bool proxy, SipMessageWriter writer, bool stale, ByteArrayPart realm)
		{
			var state = new AuthState(Interlocked.Increment(ref opaqueCount));
			authStates.Add(state.Opaque, state);

			writer.WriteAuthenticateDigest(proxy, realm,
				state.Nonce.Data1, state.Nonce.Data2, state.Nonce.Data3, state.Nonce.Data4,
				isAuthIntEnabled, stale, state.Opaque);
		}

		public ByteArrayPart GetRealm(SipMessageReader reader)
		{
			if (accounts.HasDomain(reader.RequestUri.Hostport.Host))
				return reader.RequestUri.Hostport.Host;

			if (accounts.HasDomain(reader.From.AddrSpec.Hostport.Host))
				return reader.From.AddrSpec.Hostport.Host;

			if (accounts.HasDomain(reader.To.AddrSpec.Hostport.Host))
				return reader.To.AddrSpec.Hostport.Host;

			return ByteArrayPart.Invalid;
		}

		#region IAuthorizationAgent

		AgentsState IAgent.IsAuthorized(ISheduler sheduler, ShedulerState state)
		{
			var error = Authorize(state.Reader, state.Content, AuthenticationKind.User);

			var response = sheduler.GetCommand(state, AuthSchemes.Digest, GetAuthorizationError(error));

			if (response.Command == AuthorizationCommands.TryAgain)
			{
				response.Writer.WriteXErrorDetails(GetError(error));

				var realm = GetRealm(state.Reader);
				if (realm.IsValid)
					WriteUnauthenticate(false, response.Writer, IsNonceStale(error), realm);
				else
					response.Writer.WriteXErrorDetails(GetError(ErrorCodes.InvalidDomain));
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

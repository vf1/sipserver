using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Http.Message;
using Base.Message;
using Sip.Server;
using Sip.Server.Accounts;
using Sip.Server.Users;
using SocketServers;
using ThreadSafe = System.Collections.Generic.ThreadSafe;

namespace Server.Authorization.Http
{
	using AgentsState = AuthorizationAgentsState<HttpMessageWriter>;
	using ShedulerState = AuthorizationShedulerState<HttpMessageReader>;
	using IAgent = IAuthorizationAgent<HttpMessageReader, HttpMessageWriter, AuthSchemes>;
	using ISheduler = IAuthorizationSheduler<HttpMessageReader, HttpMessageWriter, AuthSchemes>;

	class HttpDigestAuthentication
		: DigestAuthentication
		, IAuthorizationAgent<HttpMessageReader, HttpMessageWriter, AuthSchemes>
		, IDisposable
	{
		public HttpDigestAuthentication(IAccounts accounts, IUserz userz, bool isAuthIntEnabled)
			: base(accounts, userz, isAuthIntEnabled)
		{
		}

		public ErrorCodes IsAuthorizedInternal(ISheduler sheduler, ShedulerState shedulerState, AuthenticationKind kind)
		{
			var reader = shedulerState.Reader;
			var content = shedulerState.Content;

			IAccount account;
			var credentials = FindCredentials(reader, out account);

			if (credentials.AuthScheme != AuthSchemes.Digest)
				return ErrorCodes.NoResponse;

			if (credentials.AuthAlgorithm == AuthAlgorithms.Other)
				return ErrorCodes.NotSupportedAuthAlgorithm;

			if (credentials.Realm.IsInvalid)
				return ErrorCodes.RealmNotFound;

			if (credentials.Realm.Equals(shedulerState.Realm) == false)
				return ErrorCodes.InvalidRealm;

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

			if (credentials.Nonce.Length != 32)
				return ErrorCodes.NonceInvalid;

			if (credentials.Username.IsInvalid)
				return ErrorCodes.UsernmaeNotFound;

			if (sheduler.ValidateAuthorization(reader, credentials.Username, shedulerState.Param) == false)
				return ErrorCodes.NotAuthorized;

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

			var response = responseCalculator.GetResponseHexChars(
				credentials.Username,
				credentials.Realm,
				credentials.AuthAlgorithm == AuthAlgorithms.Md5Sess,
				credentials.Nonce,
				credentials.Cnonce,
				credentials.MessageQop,
				credentials.DigestUri,
				credentials.NonceCountBytes,
				reader.MethodBytes,
				content,
				new ByteArrayPart(password));

			if (credentials.Response.Equals(response) == false)
				return ErrorCodes.WrongResponse;

			state.NonceCount = credentials.NonceCount;
			state.LastAccess = unchecked(Environment.TickCount - 10000);
			authStates.Replace(state.Opaque, state);

			return ErrorCodes.Ok;
		}

		private Credentials FindCredentials(HttpMessageReader reader, out IAccount account)
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

		public void WriteAuthenticateDigest(bool proxy, HttpMessageWriter writer, bool stale, ByteArrayPart realm)
		{
			var state = new AuthState(Interlocked.Increment(ref opaqueCount));
			authStates.Add(state.Opaque, state);

			writer.WriteAuthenticateDigest(proxy, realm,
				state.Nonce.Data1, state.Nonce.Data2, state.Nonce.Data3, state.Nonce.Data4,
				isAuthIntEnabled, stale, state.Opaque);
		}

		#region IAuthorizationAgent

		AgentsState IAgent.IsAuthorized(ISheduler sheduler, ShedulerState state)
		{
			var error = IsAuthorizedInternal(sheduler, state, AuthenticationKind.User);

			var response = sheduler.GetCommand(state, AuthSchemes.Digest, GetAuthorizationError(error));

			if (response.Command == AuthorizationCommands.TryAgain)
			{
				response.Writer.WriteXErrorDetails(GetError(error));
				WriteAuthenticateDigest(false, response.Writer, IsNonceStale(error), state.Realm);
			}
			else if (response.Command == AuthorizationCommands.Cancel)
			{
				response.Writer.WriteXErrorDetails(GetError(error));
			}

			return new AuthorizationAgentsState<HttpMessageWriter>(response);
		}

		#endregion
	}
}

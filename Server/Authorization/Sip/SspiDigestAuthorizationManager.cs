using System;
using System.Collections.Generic;
using System.Text;
using Sip.Message;
using Microsoft.Win32.Ssp;
using Sip.Server;
using Server.Authorization;

namespace Server.Authorization.Sip
{
	class SspiDigestAuthorizationManager
		: IDisposable
		, IAuthorizationAgent<SipMessageReader, SipMessageWriter, AuthSchemes>
	{
		private readonly SafeCredHandle credHandle;

		public SspiDigestAuthorizationManager()
		{
			this.credHandle = Sspi.SafeAcquireCredentialsHandle(AuthSchemes.Digest.ToString(), CredentialUse.SECPKG_CRED_BOTH);
		}

		public void Dispose()
		{
			credHandle.Dispose();
		}

		#region class SecurityAssociation {...}

		protected class SecurityAssociation : IDisposable
		{
			[ThreadStatic]
			private static SecBufferDescEx secBufferDesc1;
			[ThreadStatic]
			private static SecBufferDescEx secBufferDesc2;
			[ThreadStatic]
			private static byte[] bytes1;
			[ThreadStatic]
			private static byte[] bytes2;

			private SafeCtxtHandle contextHandle;
			private bool isAuthenticationComplete;

			public SecurityAssociation()
			{
				this.contextHandle = new SafeCtxtHandle();
			}

			public void Dispose()
			{
				contextHandle.Dispose();
			}

			private static void InitializeThreadStaticVars()
			{
				secBufferDesc1 = new SecBufferDescEx(new[] { new SecBufferEx(), new SecBufferEx(), new SecBufferEx(), new SecBufferEx(), new SecBufferEx(), new SecBufferEx(), });
				secBufferDesc2 = new SecBufferDescEx(new[] { new SecBufferEx(), new SecBufferEx(), });
				bytes1 = new byte[2048];//new byte[Math.Max(Math.Max(maxTokenSize.Kerberos, maxTokenSize.Ntlm), 1024)];
				bytes2 = new byte[2048];
			}

			public bool Authentication(SafeCredHandle credHandle, Methods method, byte[] realm, byte[] inToken, out ArraySegment<byte> outToken)
			{
				//idleTime = DateTime.UtcNow.AddMinutes(IdleMinutes);

				InitializeThreadStaticVars();

				secBufferDesc1.Buffers[0].SetBuffer(BufferType.SECBUFFER_TOKEN, inToken);
				secBufferDesc1.Buffers[1].SetBuffer(BufferType.SECBUFFER_PKG_PARAMS, method.ToByteArrayPart().Bytes);
				secBufferDesc1.Buffers[2].SetBuffer(BufferType.SECBUFFER_PKG_PARAMS, new byte[0]);
				secBufferDesc1.Buffers[3].SetBuffer(BufferType.SECBUFFER_PKG_PARAMS, new byte[0]);
				secBufferDesc1.Buffers[4].SetBuffer(BufferType.SECBUFFER_PKG_PARAMS, realm);
				secBufferDesc1.Buffers[5].SetBuffer(BufferType.SECBUFFER_CHANNEL_BINDINGS, new byte[0]);

				secBufferDesc2.Buffers[0].SetBuffer(BufferType.SECBUFFER_TOKEN, bytes1);
				secBufferDesc2.Buffers[1].SetBufferEmpty();

				var newHandle = (contextHandle.IsInvalid) ? new SafeCtxtHandle() : contextHandle;

				var result = Sspi.SafeAcceptSecurityContext(
					ref credHandle,
					ref contextHandle,
					ref secBufferDesc1,
					0,
					TargetDataRep.SECURITY_NETWORK_DREP,
					ref newHandle,
					ref secBufferDesc2);

				Tracer.WriteInformation("SSPI Digest Auth: " + result.ToString());

				if (contextHandle.IsInvalid && newHandle.IsInvalid == false)
					contextHandle = newHandle;

				if (result == SecurityStatus.SEC_E_OK)
				{
					outToken = new ArraySegment<byte>();
					isAuthenticationComplete = true;

					return true;
				}
				else
				{
					outToken = new ArraySegment<byte>();
					return false;
				}
			}

			public bool IsAuthenticationComplete
			{
				get { return isAuthenticationComplete; }
			}
		}

		#endregion

		private SecurityAssociation sa;

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
			DigestUriNotFound,
			ResponseNotFound,
			InvalidRealm,
			NoResponse,
		}

		#region IAuthorizationAgent

		AuthorizationAgentsState<SipMessageWriter> IAuthorizationAgent<SipMessageReader, SipMessageWriter, AuthSchemes>.IsAuthorized(IAuthorizationSheduler<SipMessageReader, SipMessageWriter, AuthSchemes> sheduler, AuthorizationShedulerState<SipMessageReader> state)
		{
			//int opaque;
			//bool proxy;
			//ArraySegment<byte> token;
			//var error = Authorize(state.Message.Reader, scheme, out token, out opaque, out proxy);

			//AuthorizationError error1;
			//switch (error)
			//{
			//    case ErrorCodes.Ok:
			//        error1 = AuthorizationError.Success;
			//        break;
			//    case ErrorCodes.Continue:
			//        error1 = AuthorizationError.Continue;
			//        break;
			//    case ErrorCodes.NoResponse:
			//        error1 = AuthorizationError.None;
			//        break;
			//    default:
			//        error1 = AuthorizationError.Failed;
			//        break;
			//}

			var realm1 = "officesip.local";
			var error = ErrorCodes.NoResponse;
			if (state.Reader.Count.AuthorizationCount > 0)
			{

				//if (credentials.AuthScheme != AuthSchemes.Digest)
				//            var credentials = state.Message.Reader.GetCredentialsByRealm(AuthSchemes.Digest, new ByteArrayPart(realm1));
			}
			ArraySegment<byte> outToken;

			if (sa == null)
			{
				sa = new SecurityAssociation();
				sa.Authentication(credHandle, state.Reader.Method, Encoding.ASCII.GetBytes(realm1), null, out outToken);
			}

			var response = sheduler.GetCommand(state, AuthSchemes.Digest, AuthorizationError.Failed); //error1);

			//if (response.Command == AuthorizationCommands.TryAgain)
			//{
			//    response.Writer.WriteAuthenticateMs(proxy, scheme, targetname, realm, opaque);
			//    response.Writer.WriteXErrorDetails(GetError(error));
			//    response.Writer.WriteDate(DateTime.UtcNow);
			//}
			//else if (response.Command == AuthorizationCommands.Continue)
			//{
			//    response.Writer.WriteAuthenticateMs(proxy, scheme, targetname, realm, opaque, token);
			//    response.Writer.WriteDate(DateTime.UtcNow);
			//}
			//else if (response.Command == AuthorizationCommands.Cancel)
			//{
			//    response.Writer.WriteXErrorDetails(GetError(error));
			//}

			return new AuthorizationAgentsState<SipMessageWriter>(response);
		}

		#endregion
	}
}

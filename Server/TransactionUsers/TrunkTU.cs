using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using Sip.Message;
using Sip.Tools;
using DnDns.Enums;
using DnDns.Query;
using DnDns.Records;
using DnDns.Security;
using System.Text.RegularExpressions;
using Server.Authorization.Sip;

namespace Sip.Server
{
	class TrunkTU
		: BaseTransactionUser
		, IDisposable
	{
		private ProducedRequest registerProducer;
		private TrunkManager trunkManager;
		private static int count = Environment.TickCount * 71 + 37;
		private MultiTimerEx<int> registerTimer;

		public TrunkTU(TrunkManager trunkManager)
		{
			this.registerTimer = new MultiTimerEx<int>(RegisterTimer, 64);

			this.trunkManager = trunkManager;
			this.trunkManager.TrunkAdded += TrunkManager_TrunkAdded;
			this.trunkManager.TrunkRemoved += TrunkManager_TrunkRemoved;
			this.trunkManager.TrunkUpdated += trunkManager_TrunkUpdated;

			this.registerProducer = new ProducedRequest(this)
			{
				IncomingResponse = ProccessResponse,
				TransportError = ProccessTransportError,
			};
		}

		public void Dispose()
		{
			trunkManager.TrunkAdded -= TrunkManager_TrunkAdded;
			trunkManager.TrunkRemoved -= TrunkManager_TrunkRemoved;
			trunkManager.TrunkUpdated -= trunkManager_TrunkUpdated;
		}

		public override IEnumerable<ProducedRequest> GetProducedRequests()
		{
			yield return registerProducer;
		}

		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			return new AcceptedRequest[0];
		}

		private void TrunkManager_TrunkAdded(Trunk trunk)
		{
			if (trunk.State != Trunk.States.Error)
				RegisterTrunk(trunk);
		}

		private void TrunkManager_TrunkRemoved(Trunk trunk)
		{
			trunk.State = Trunk.States.Disconnecting;
			SendRegister(trunk, 0);
		}

		private void trunkManager_TrunkUpdated(Trunk trunk)
		{
			if (trunk.State == Trunk.States.Error)
				if (trunk.RegisterAfterErrorTimeout > 0)
					registerTimer.Add(trunk.RegisterAfterErrorTimeout * 1000, trunk.Id);
		}

		private void RegisterTrunk(Trunk trunk)
		{
			trunk.State = Trunk.States.WaitingDns;

			var match = Regex.Match(trunk.OutgoingProxy, "(?<url>[^:]+)(:(?<port>[0-9]*))?");
			if (match.Success == false)
			{
				trunk.ErrorMessage = @"Invalid outgouing proxy URL";
			}
			else
			{
				var hostname = match.Groups["url"].Value;
				int? port = null;
				if (match.Groups["port"].Success)
					port = int.Parse(match.Groups["port"].Value);

				IPAddress address;
				if (IPAddress.TryParse(hostname, out address))
				{
					trunk.SetServerEndPoint(address, port ?? 5060);
					trunk.State = Trunk.States.Waiting200or401;
					SendRegister(trunk, trunk.RegisterDuration);
				}
				else
				{
					try
					{
						var prefixes = new string[] { @"_sip._tcp.", @"_sip._udp." };

						Transports transport = Transports.None;
						SrvRecord record = null;
						foreach (var prefix in prefixes)
						{
							var request = new DnsQueryRequest();
							var response = request.Resolve(prefix + hostname, NsType.SRV, NsClass.INET, ProtocolType.Udp);

							if (response.Answers.Length > 0)
								record = response.Answers[0] as SrvRecord;

							if (record != null)
							{
								if (prefix.Contains(@"tcp"))
									transport = Transports.Tcp;
								if (prefix.Contains(@"udp"))
									transport = Transports.Udp;
								break;
							}
						}

						if (record == null)
							trunk.ErrorMessage = @"Failed to get SRV record";
						else
						{
							var addresses = Dns.GetHostAddresses(record.HostName);
							if (addresses.Length < 0)
								trunk.ErrorMessage = @"Failed to get DNS A record for " + record.HostName;
							else
							{
								trunk.SetServerEndPoint(transport, addresses[0], port ?? record.Port);
								trunk.State = Trunk.States.Waiting200or401;
								SendRegister(trunk, trunk.RegisterDuration);
							}
						}
					}
					catch (Exception ex)
					{
						trunk.ErrorMessage = ex.Message;
					}
				}
			}
		}

		private void SendRegister(Trunk trunk, int expires)
		{
			var writer = GetWriter();

			int transationId = GetTransactionId(Methods.Registerm);

			writer.WriteRequestLine(Methods.Registerm, trunk.RequestUri);
			writer.WriteVia(trunk.Transport, trunk.LocalEndPoint, transationId);
			writer.WriteFrom(trunk.Uri, trunk.Id);
			writer.WriteTo(trunk.Uri);
			writer.WriteCallId(trunk.LocalEndPoint.Address, Interlocked.Increment(ref count));
			writer.WriteCseq(trunk.GetCSeq(Methods.Registerm), Methods.Registerm);
			writer.WriteContact(trunk.LocalEndPoint, trunk.Transport);
			writer.WriteEventRegistration();
			writer.WriteExpires(expires);

			if (trunk.Nonce.IsValid)
			{
				int nc = trunk.GetNextNonceCount();
				int cnonce = Environment.TickCount;

				var response = SipDigestAuthentication.GetResponseHexChars(trunk.AuthenticationId, trunk.Realm, AuthAlgorithms.Md5, trunk.Nonce,
					cnonce, nc, trunk.Password, trunk.Qop, trunk.RequestUri,
					Methods.Registerm.ToByteArrayPart(), new ArraySegment<byte>());

				writer.WriteDigestAuthorization(trunk.AuthHeader, trunk.AuthenticationId, trunk.Realm, trunk.Qop, AuthAlgorithms.Md5, trunk.RequestUri,
					trunk.Nonce, nc, cnonce, trunk.Opaque, response);
			}

			writer.WriteContentLength(0);
			writer.WriteCRLF();

			registerProducer.SendRequest(trunk.ConnectionAddresses, writer, transationId, trunk.Id);
		}

		private void ProccessResponse(IncomingMessageEx response)
		{
			int trunkId;
			var tag = response.Reader.From.Tag;

			if (tag.Length == 8 && HexEncoding.TryParseHex8(tag.Bytes, tag.Begin, out trunkId))
			{
				var trunk = trunkManager.GetTrunkById(trunkId);
				if (trunk != null)
				{
					switch (response.Reader.StatusCode.Value)
					{
						case 401:
						case 407:
							if ((trunk.State == Trunk.States.Waiting200or401 || trunk.State == Trunk.States.Connected) &&
								(response.Reader.Count.WwwAuthenticateCount > 0 || response.Reader.Count.ProxyAuthenticateCount > 0))
							{
								Challenge challenge;
								if (response.Reader.Count.WwwAuthenticateCount > 0)
								{
									challenge = response.Reader.WwwAuthenticate[0];
									trunk.AuthHeader = HeaderNames.Authorization;
								}
								else
								{
									challenge = response.Reader.ProxyAuthenticate[0];
									trunk.AuthHeader = HeaderNames.ProxyAuthorization;
								}

								if (challenge.AuthScheme == AuthSchemes.Digest)
								{
									trunk.State = Trunk.States.Waiting200;
									trunk.UpdateChallenge(challenge);
									SendRegister(trunk, trunk.RegisterDuration);
								}
								else
								{
									trunk.ErrorMessage = @"Unsupport Auth Scheme";
								}
							}
							else
							{
								trunk.ErrorMessage = @"Proxy response: " + response.Reader.StatusCode.Value.ToString();
							}
							break;

						case 200:
							if (trunk.State == Trunk.States.Disconnecting)
								trunk.State = Trunk.States.Disconnected;
							else
							{
								trunk.State = Trunk.States.Connected;
								int repeat = response.Reader.GetExpires(0) - 10;
								repeat = (repeat <= 0) ? 1 : repeat;
								registerTimer.Add(repeat * 1000, trunk.Id);
							}
							break;

						default:
							trunk.ErrorMessage = @"Proxy response: " + response.Reader.StatusCode.Value.ToString();
							break;
					}
				}
			}
		}

		public void RegisterTimer(int trunkId)
		{
			var trunk = trunkManager.GetTrunkById(trunkId);
			if (trunk != null)
			{
				if (trunk.State == Trunk.States.Connected)
					SendRegister(trunk, trunk.RegisterDuration);
				if (trunk.State == Trunk.States.Error)
					RegisterTrunk(trunk);
			}
		}

		private void ProccessTransportError(int clientTransactionId, int trunkId)
		{
			var trunk = trunkManager.GetTrunkById(trunkId);
			if (trunk != null)
				trunk.ErrorMessage = @"Transport Error";
		}
	}
}

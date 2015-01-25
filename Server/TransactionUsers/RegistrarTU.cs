using System;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;
using Sip.Server.Accounts;

namespace Sip.Server
{
	sealed class RegistrarTU
		: BaseTransactionUser
	{
		private const int minExpires = 180;
		private const int maxExpires = 3600;
		private const int defaultExpires = 1200;
		private readonly LocationService locationService;
		private readonly IAccounts accounts;

		public RegistrarTU(LocationService locationService, IAccounts accounts)
		{
			this.locationService = locationService;
			this.accounts = accounts;
		}

		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			yield return new AcceptedRequest(this)
			{
				Method = Methods.Registerm,
				IsAcceptedRequest = IsAccepted,
				IncomingRequest = ProccessRegister,
			};
		}

		private bool IsAccepted(SipMessageReader reader)
		{
			var host = reader.RequestUri.Hostport.Host;

			return accounts.HasDomain(host) || IsLocalAddress(host.ToIPAddress());
		}

		private void ProccessRegister(AcceptedRequest tu, IncomingMessageEx request)
		{
			StatusCodes statusCode = StatusCodes.OK;

			bool isStar = HasStarContact(request.Reader);
			var user = request.Reader.To.AddrSpec.User;
			var domain = request.Reader.RequestUri.Hostport.Host;

			if (isStar)
			{
				if (request.Reader.Count.ContactCount > 1 && request.Reader.Expires != 0)
					statusCode = StatusCodes.BadRequest;
			}
			else
			{
				if (IsExpiresValid(request.Reader) == false)
					statusCode = StatusCodes.IntervalTooBrief;
			}

			if (statusCode == StatusCodes.OK)
			{
				if (isStar)
					locationService.RemoveAllBindings(user, domain);
				else
					if (locationService.UpdateBindings(user, domain, request, defaultExpires) == false)
						statusCode = StatusCodes.Gone;
			}

			var writer = GetWriter();

			if (statusCode == StatusCodes.OK)
			{
				writer.WriteStatusLine(StatusCodes.OK);
				writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, StatusCodes.OK);

				int maxExpires = 0;

				foreach (var binding in locationService.GetEnumerableBindings(user, domain))
				{
					writer.WriteContact(binding.AddrSpec, binding.SipInstance, binding.Expires);

					if (maxExpires < binding.Expires)
						maxExpires = binding.Expires;
				}

				writer.WriteExpires(maxExpires);
				writer.WriteCustomHeaders();
				writer.WriteContentLength(0);
				writer.WriteCRLF();
			}
			else
			{
				writer.WriteStatusLine(statusCode);
				writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, statusCode);
				if (statusCode == StatusCodes.IntervalTooBrief)
					writer.WriteMinExpires(minExpires);
				writer.WriteContentLength(0);
				writer.WriteCRLF();
			}

			tu.SendResponse(request, writer);
		}

		private bool IsExpiresValid(SipMessageReader reader)
		{
			int expires = reader.GetExpires(defaultExpires, maxExpires);

			for (int i = 0; i < reader.Count.ContactCount; i++)
			{
				int localExpires = reader.Contact[i].Expires;

				if (localExpires == int.MinValue)
					localExpires = expires;

				if (localExpires != int.MinValue && localExpires > 0 && localExpires < minExpires)
					return false;
			}

			return true;
		}

		private bool HasStarContact(SipMessageReader reader)
		{
			for (int i = 0; i < reader.Count.ContactCount; i++)
			{
				if (reader.Contact[i].IsStar)
					return true;
			}

			return false;
		}
	}
}

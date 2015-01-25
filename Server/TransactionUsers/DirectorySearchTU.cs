using System;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;
using Sip.Server.Accounts;
using Sip.Server.Users;
using ServiceSoap;
using ServiceSoap.XmlContent;

using System.Linq;

namespace Sip.Server
{
	sealed class DirectorySearchTU
		: BaseTransactionUser
	{
		private readonly ByteArrayPart type;
		private readonly ByteArrayPart subtype;
		private readonly ServiceSoap1 serviceSoap;
		private readonly IUserz userz;
		private readonly IAccounts accounts;

		public DirectorySearchTU(IAccounts accounts, ServiceSoap1 serviceSoap, IUserz userz)
		{
			this.type = new ByteArrayPart("application");
			this.subtype = new ByteArrayPart("SOAP+xml");
			this.accounts = accounts;
			this.serviceSoap = serviceSoap;
			this.userz = userz;
		}

		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			yield return new AcceptedRequest(this)
			{
				Method = Methods.Servicem,
				IsAcceptedRequest = (reader) =>
					AcceptedRequest.IsValidContentType(reader, type, subtype) &&
					AcceptedRequest.IsToUserEmpty(reader),
				IncomingRequest = ProccessService,
			};
		}

		private void ProccessService(AcceptedRequest tu, IncomingMessageEx request)
		{
			var account = accounts.GetAccount(request.Reader.RequestUri.Hostport.Host);

			var writer = GetWriter();

			if (account != null)
			{
				var outContent = serviceSoap.ProcessRequest<IAccount>(request.Content, Search, account);

				writer.WriteStatusLine(StatusCodes.OK);
				writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, StatusCodes.OK);
				writer.WriteContentType(type, subtype);
				writer.WriteCustomHeaders();
				writer.WriteContentLength();
				writer.WriteCRLF();

				writer.Write(outContent.GenerateToByteArray());
				writer.RewriteContentLength();
			}
			else
			{
				writer.WriteStatusLine(StatusCodes.NotFound);
				writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, StatusCodes.NotFound);
				writer.WriteContentLength(0);
				writer.WriteCRLF();
			}

			tu.SendResponse(request, writer);
		}

		private DirectorySearchResponse Search(ServiceSoap1 service, DirectorySearchRequest request, IAccount account)
		{
			var count = service.MaxResults > request.MaxResults ? request.MaxResults : service.MaxResults;

			string givenName, givenEmail;
			if (request.SearchTerms.TryGetValue(@"givenName", out givenName) == false)
				givenName = string.Empty;
			if (request.SearchTerms.TryGetValue(@"givenEmail", out givenEmail) == false)
				givenEmail = string.Empty;

			var result = new List<DirectorySearchItem>(count);
			var moreAvailable = false;

			for (int i = 0; i < userz.Count; i++)
			{
				if (Search(account, userz[i], givenName, givenEmail, count, result))
				{
					moreAvailable = true;
					break;
				}
			}

			return new DirectorySearchResponse()
			{
				Items = result,
				MoreAvailable = moreAvailable,
			};
		}

		private bool Search(IAccount account, IUsers users, string name, string email, int maxCount, IList<DirectorySearchItem> result)
		{
			const int sliceSize = 100;

			for (int i = 0; i < users.GetCount(account.Id); i += sliceSize)
			{
				foreach (var item in users.GetUsers(account.Id, i, sliceSize))
				{
					if ((name == ""
						|| !string.IsNullOrEmpty(item.DisplayName) && item.DisplayName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) &&
						(email == ""
						|| !string.IsNullOrEmpty(item.Email) && item.Email.IndexOf(email, StringComparison.OrdinalIgnoreCase) >= 0))
					{
						result.Add(
							   new DirectorySearchItem()
							   {
								   Uri = item.Name + @"@" + account.DomainName,
								   DisplayName = item.DisplayName,
								   Title = item.Title,
								   Office = item.PhysicalDeliveryOfficeName,
								   Phone = item.Telephone,
								   Company = item.Company,
								   City = item.City,
								   State = item.State,
								   Country = item.CountryCode,
								   Email = item.Email
							   });

					}

					if (result.Count >= maxCount)
						return true;
				}
			}

			return false;
		}
	}
}

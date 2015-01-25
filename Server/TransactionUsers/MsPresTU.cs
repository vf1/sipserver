using System;
using System.Text;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;
using Sip.Server.Accounts;
using Sip.Server.Users;
using SocketServers;
using EnhancedPresence;

namespace Sip.Server
{
	sealed class MsPresTU
		: BaseTransactionUser
	{
		private readonly EnhancedPresence1 enhancedPresence;
		private readonly DialogManager dialogManager;
		private readonly IUserz userz;
		private readonly IAccounts accounts;
		private readonly LocationService locationService;

		private readonly static ByteArrayPart serviceContentType = new ByteArrayPart(Categories.InContentType);
		private readonly static ByteArrayPart serviceContentSubtype = new ByteArrayPart(Categories.InContentSubtype);
		private readonly static ByteArrayPart subscribeContentType = new ByteArrayPart(BatchSubscribe.InContentType);
		private readonly static ByteArrayPart subscribeContentSubtype = new ByteArrayPart(BatchSubscribe.InContentSubtype);
		private readonly static byte[] UCCAPI = Encoding.UTF8.GetBytes("UCCAPI");
		private readonly static ByteArrayPart contentType = new ByteArrayPart("application/rlmi+xml");
		private readonly static ByteArrayPart eventlist = new ByteArrayPart("eventlist");

		public MsPresTU(IAccounts accounts, IUserz userz, LocationService locationService)
		{
			this.accounts = accounts;
			this.userz = userz;
			this.locationService = locationService;
			this.enhancedPresence = new EnhancedPresence1(BenotifyHandler);
			this.dialogManager = new DialogManager();

			locationService.AorAdded += LocationService_AorAdded;
			locationService.AorRemoved += LocationService_AorRemoved;
			locationService.ContactAdded += LocationService_ContactAdded;
			locationService.ContactRemoved += LocationService_ContactRemoved;

			userz.Updated += Users_AddedOrUpdated;
			userz.Added += Users_AddedOrUpdated;
			userz.Reset += Users_Reset;

			accounts.ForEach((account) =>
			{
				for (int i = 0; i < userz.Count; i++)
					ResetUsers(userz[i], account);
			});
		}

		public void Dispose()
		{
			locationService.AorAdded -= LocationService_AorAdded;
			locationService.AorRemoved -= LocationService_AorRemoved;
			locationService.ContactAdded -= LocationService_ContactAdded;
			locationService.ContactRemoved -= LocationService_ContactRemoved;

			userz.Updated -= Users_AddedOrUpdated;
			userz.Added -= Users_AddedOrUpdated;
			userz.Reset -= Users_Reset;
		}

		public EnhancedPresence1 EnhancedPresence
		{
			get { return enhancedPresence; }
		}

		public override IEnumerable<AcceptedRequest> GetAcceptedRequests()
		{
			yield return new AcceptedRequest(this)
			{
				Method = Methods.Servicem,
				IsAcceptedRequest = (reader) =>
					AcceptedRequest.IsAccepted(reader, serviceContentType, serviceContentSubtype, true),
				IncomingRequest = ProccessService,
			};

			yield return new AcceptedRequest(this)
			{
				Method = Methods.Subscribem,
				IsAcceptedRequest = (reader) =>
					AcceptedRequest.IsAccepted(reader, subscribeContentType, subscribeContentSubtype, true),
				IncomingRequest = ProccessSubscribe,
			};

			yield return new AcceptedRequest(this)
			{
				Method = Methods.Subscribem,
				IsAcceptedRequest = IsUnsubscribeAccepted,
				IncomingRequest = ProccessUnsubscribe,
			};
		}

		private bool IsUnsubscribeAccepted(SipMessageReader reader)
		{
			return reader.Expires == 0 && reader.UserAgent.Product.StartsWith(UCCAPI);
		}

		private void ProccessService(AcceptedRequest tu, IncomingMessageEx request)
		{
			var writer = GetWriter();
			StatusCodes statusCode = StatusCodes.OK;

			string endpointId = null;
			if (request.Reader.Count.ContactCount == 0)
				statusCode = StatusCodes.BadRequest;
			else
				endpointId = (request.Reader.Contact[0].SipInstance.IsValid == true ?
					request.Reader.Contact[0].SipInstance : request.Reader.Contact[0].AddrSpec1.Value).ToString();

			if (statusCode == StatusCodes.OK)
			{
				try
				{
					var categories = enhancedPresence.ParsePublication(request.Content);

					var content = enhancedPresence.ProcessPublication(
						request.Reader.From.AddrSpec.Value.ToString(), categories, endpointId);

					writer.WriteStatusLine(statusCode);
					writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, statusCode);
					writer.WriteContentType(new ByteArrayPart(categories.OutContentType), new ByteArrayPart(categories.OutContentSubtype));
					// нужен ли контакт
					writer.WriteCustomHeaders();
					writer.WriteContentLength();
					writer.WriteCRLF();

					int endOfHeaders = writer.Count;
					writer.Write(content.GenerateToByteArray());
					writer.RewriteContentLength(writer.Count - endOfHeaders);
				}
				catch (EnhancedPresenceException)
				{
					statusCode = StatusCodes.BadRequest;
				}
			}

			if (statusCode != StatusCodes.OK)
			{
				writer.WriteResponse(request.Reader, statusCode);
			}

			tu.SendResponse(request, writer);
		}

		private void ProccessSubscribe(AcceptedRequest tu, IncomingMessageEx request)
		{
			StatusCodes statusCode = StatusCodes.OK;
			string endpointId = string.Empty;

			int expires = request.Reader.GetExpires(600, 900);

			if (request.Reader.IsExpiresTooBrief(60))
				statusCode = StatusCodes.IntervalTooBrief;

			if (statusCode == StatusCodes.OK)
			{
				if (request.Reader.Count.ContactCount == 0)
					statusCode = StatusCodes.BadRequest;
				else
					endpointId = (request.Reader.Contact[0].SipInstance.IsValid == true ?
						request.Reader.Contact[0].SipInstance : request.Reader.Contact[0].AddrSpec1.Value).ToString();
			}

			Dialog dialog = null;
			if (statusCode == StatusCodes.OK)
				dialog = dialogManager.GetOrCreate(request.Reader, request.ConnectionAddresses, out statusCode);

			var writer = GetWriter();

			if (statusCode == StatusCodes.OK)
			{
                try
                {
                    var batchSubscribe = enhancedPresence.ParseSubscribe(request.Content);

                    var contents = enhancedPresence.ProcessSubscription(
                        request.Reader.From.AddrSpec.Value.ToString(),
                        dialog.Id.ToString(),
                        batchSubscribe,
                        (uint)expires,
                        endpointId,
                        dialog);

                    writer.WriteStatusLine(statusCode);
                    writer.CopyViaToFromCallIdRecordRouteCSeq(request.Reader, statusCode, dialog.LocalTag);
                    writer.WriteContact(dialog.LocalEndPoint, dialog.Transport);
                    writer.WriteExpires(expires);
                    writer.WriteEventPresence();
                    writer.WriteSupportedMsBenotify();
                    writer.WriteContentTypeMultipart(contentType);
                    writer.WriteCustomHeaders();
                    writer.WriteContentLength();
                    writer.WriteCRLF();

                    WriteResponseContent(writer, contents);
                }
                catch (EnhancedPresenceException)
                {
                    // TODO: add error info in response
                    statusCode = StatusCodes.BadRequest;
                }
			}

            if (statusCode != StatusCodes.OK)
            {
				writer.WriteResponse(request.Reader, statusCode);
			}

			tu.SendResponse(request, writer);
		}

		private void ProccessUnsubscribe(AcceptedRequest tu, IncomingMessageEx request)
		{
			var writer = GetWriter();

			var dialog = dialogManager.Get(request.Reader);

			if (dialog != null)
			{
				writer.WriteResponse(request.Reader, StatusCodes.OK, dialog.LocalTag);
				dialogManager.Remove(dialog.Id);
			}
			else
			{
				writer.WriteResponse(request.Reader, StatusCodes.OK);
			}

			tu.SendResponse(request, writer);
		}

		private void WriteResponseContent(SipResponseWriter writer, List<OutContent> contents)
		{
			writer.WriteCRLF();
			foreach (var c in contents)
			{
				writer.WriteBoundary();
				writer.WriteContentTransferEncodingBinary();
				writer.WriteContentType(new ByteArrayPart(c.OutContentType), new ByteArrayPart(c.OutContentSubtype));
				writer.WriteCRLF();

				writer.Write(c.GenerateToByteArray());

				writer.WriteCRLF();
				writer.WriteCRLF();
			}

			writer.WriteBoundaryEnd();
			writer.RewriteContentLength();
		}

		private void BenotifyHandler(uint expires, string subscriptionState, OutContent content, Object param)
		{
			var dialog = param as Dialog;
			var writer = GetWriter();

			if (expires <= 0)
				dialogManager.Remove(dialog.Id);

			writer.WriteRequestLine(Methods.Benotifym, dialog.RemoteUri);
			writer.WriteVia(dialog.Transport, dialog.LocalEndPoint);
			writer.WriteFrom(dialog.LocalUri, dialog.LocalTag);
			writer.WriteTo(dialog.RemoteUri, dialog.RemoteTag, dialog.Epid);

			writer.WriteCallId(dialog.CallId);
			writer.WriteEventPresence();
			writer.WriteSubscriptionState((int)expires);
			writer.WriteMaxForwards(70);
			writer.WriteCseq(dialog.GetNextLocalCseq(), Methods.Benotifym);
			writer.WriteContact(dialog.LocalEndPoint, dialog.Transport);
			writer.WriteExpires((int)expires);
			writer.WriteRequire(eventlist);
			writer.WriteCustomHeaders();

			writer.WriteContentType(new ByteArrayPart(content.OutContentType), new ByteArrayPart(content.OutContentSubtype));

			var rawContent = content.GenerateToByteArray();

			writer.WriteContentLength(rawContent.Length);

			writer.WriteCRLF();

			writer.Write(rawContent);

			SendNonTransactionMessage(dialog.Transport, dialog.LocalEndPoint, dialog.RemoteEndPoint, ServerAsyncEventArgs.AnyConnectionId, writer);
		}

		private void Users_Reset(int accountId, IUsers users)
		{
			var account = accounts.GetAccount(accountId);
			if (account != null)
				ResetUsers(users, account);
		}

		private void Users_AddedOrUpdated(int accountId, IUsers source, IUser user)
		{
			var account = accounts.GetAccount(accountId);
			if (account != null)
				AddOrUpdateUser(user, GetUri(user.Name, account.DomainName));
		}

		private string GetUri(string user, string domain)
		{
			return @"sip:" + user + @"@" + domain;
		}

		private void ResetUsers(IUsers users, IAccount account)
		{
			for (int i = 0; i < users.GetCount(account.Id); i += 100)
			{
				foreach (var user in users.GetUsers(account.Id, i, 100))
					AddOrUpdateUser(user, GetUri(user.Name, account.DomainName));
			}
		}

		private void AddOrUpdateUser(IUser user, string uri)
		{
			enhancedPresence.SetContactCard(uri, new ContactCardCategory()
			{
				DisplayName = user.DisplayName,
				Email = user.Email
			});


			List<UserPropertiesCategory.Line> lines = null;
			if (String.IsNullOrEmpty(user.Telephone) == false)
			{
				lines = new List<UserPropertiesCategory.Line>(1);
				lines.Add(new UserPropertiesCategory.Line()
				{
					Value = @"tel:" + user.Telephone,
					LineType = UserPropertiesCategory.LineType.Uc
				});
			}

			enhancedPresence.SetUserProperties(uri, new UserPropertiesCategory()
			{
				Lines = lines,
				FaxNumber = user.Fax,
				StreetAddress = user.StreetAddress,
				City = user.City,
				State = user.State,
				CountryCode = user.CountryCode,
				PostalCode = user.PostalCode,
				WwwHomePage = user.WwwHomepage
			});
		}

		#region Legacy Code

		///// <summary>
		///// Установка источника данных пользователей.
		///// </summary>
		//public void SetUsersProvider(SIPServer.IUsersOld users, string domain)
		//{
		//    users.evAORUpdated += UPAORUpdated;

		//    foreach (var i in users.GetAORs())
		//        UPAORUpdated(this, new SIPServer.EventArgs<string, SIPServer._IUsers.AOR>(i.Name + @"@" + domain, i));
		//}

		/// <summary>
		/// Установка Location Service.
		/// </summary>
		//public void SetLocationService(LocationService service)
		//{
		//    service.AorAdded += LocationService_AorAdded;
		//    service.AorRemoved += LocationService_AorRemoved;

		//    service.ContactAdded += LocationService_ContactAdded;
		//    service.ContactRemoved += LocationService_ContactRemoved;
		//}

		///// <summary>
		///// Обработчик события AORUpdated от источник данных пользователей.
		///// </summary>
		//private void UPAORUpdated(Object sender, SIPServer.EventArgs<string, SIPServer._IUsers.AOR> e)
		//{
		//    var uri = @"sip:" + e.value1;

		//    enhancedPresence.SetContactCard(uri, new ContactCardCategory()
		//    {
		//        DisplayName = e.value2.DisplayName,
		//        Email = e.value2.EMail
		//    });

		//    if (e.value2 is SIPServer._IUsers.ADAOR)
		//    {
		//        var ad = e.value2 as SIPServer._IUsers.ADAOR;

		//        List<UserPropertiesCategory.Line> lines = null;
		//        if (String.IsNullOrEmpty(ad.telephoneNumber) == false)
		//        {
		//            lines = new List<UserPropertiesCategory.Line>(1);
		//            lines.Add(new UserPropertiesCategory.Line()
		//            {
		//                Value = @"tel:" + ad.telephoneNumber,
		//                LineType = UserPropertiesCategory.LineType.Uc
		//            });
		//        }

		//        enhancedPresence.SetUserProperties(uri, new UserPropertiesCategory()
		//        {
		//            Lines = lines,
		//            FaxNumber = ad.facsimileTelephoneNumber,
		//            StreetAddress = ad.streetAddress,
		//            City = ad.l,
		//            State = ad.st,
		//            CountryCode = ad.countryCode,
		//            PostalCode = ad.postalCode,
		//            WwwHomePage = ad.wWWHomePage
		//        });
		//    }
		//}

		#endregion

		#region LocationService Events

		private void LocationService_AorAdded(ByteArrayPart aor)
		{
			enhancedPresence.UserRegistered(aor.ToString());
		}

		private void LocationService_AorRemoved(ByteArrayPart aor)
		{
			enhancedPresence.UserUnregistered(aor.ToString());
		}

		/// <summary>
		/// Обработчик события evRegisteredAORContact от Location Service.
		/// </summary>
		private void LocationService_ContactAdded(ByteArrayPart aor1, LocationService.Binding contact, SipMessageReader request)
		{
			var aor = aor1.ToString();
			bool ep = false;

			for (var i = 0; i < request.Count.SupportedCount; i++)
			{
				if (request.Supported[i].Option.ToString() == @"msrtc-event-categories")
				{
					ep = true;
					break;
				}
			}

			enhancedPresence.EndpointRegistered(aor, contact.SipInstance.Length != 0 ? contact.SipInstance.ToString() : contact.AddrSpec.ToString(), ep);
		}

		private void LocationService_ContactRemoved(ByteArrayPart aor1, LocationService.Binding binding)
		{
			enhancedPresence.EndpointUnregistered(aor1.ToString(),
				binding.SipInstance.IsValid ? binding.SipInstance.ToString() : binding.AddrSpec.ToString());
		}

		#endregion
	}
}

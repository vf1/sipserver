using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Sip.Server;
using Base.Message;
using Http.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sip.Server.WcfService;
using Sip.Server.Users;
using Sip.Server.Accounts;
using Sip.Server.Configuration;
using SocketServers;
using HttpMethods = Http.Message.Methods;
using Server.Http;

namespace Server.Restapi
{
    class RestapiService
        : IHttpServerAgent
    {
        [ThreadStatic]
        private static RestapiUriParser parser;
        [ThreadStatic]
        private static HMAC hmac;

        private static readonly byte[] restApiPrefix = Encoding.UTF8.GetBytes("/api");
        private static readonly Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        private IHttpServer httpServer;
        private readonly Accountx accounts;
        private readonly IUserz userz;

        public RestapiService(Accountx accounts, Userz userz)
        {
            this.accounts = accounts;
            this.userz = userz;
        }

        public void Dispose()
        {
        }

        public string AdministratorPassword
        {
            get;
            set;
        }

        IHttpServer IHttpServerAgent.IHttpServer
        {
            set { httpServer = value; }
        }

        HttpServerAgent.IsHandledResult IHttpServerAgent.IsHandled(HttpMessageReader httpReader)
        {
            return httpReader.RequestUri.StartsWith(restApiPrefix);
        }

        bool IHttpServerAgent.IsAuthorized(HttpMessageReader reader, ByteArrayPart username)
        {
            return true;
        }

        void IHttpServerAgent.HandleRequest(BaseConnection c, HttpMessageReader httpReader, ArraySegment<byte> httpContent)
        {
            InitializeThreadStaticVariables();

            parser.SetDefaultValue();
            parser.ParseAll(httpReader.RequestUri.ToArraySegment());

            if (parser.IsFinal)
            {
                parser.SetArray(httpReader.RequestUri.Bytes);
                ExecuteInternal(c, httpReader, httpContent);
            }
            else
            {
                SendResponse(c, StatusCodes.BadRequest);
            }
        }

        private void InitializeThreadStaticVariables()
        {
            if (parser == null)
                parser = new RestapiUriParser();
            if (hmac == null)
                hmac = new HMACMD5();
        }

        #region static class Json {...}

        static class Json
        {
            private static readonly JsonSerializerSettings serializeSettings =
                new JsonSerializerSettings { ContractResolver = new JsonContractResolver(), };
            private static readonly JsonSerializerSettings deserializeSettings =
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), };

            public static string SerializeObject(object value)
            {
                return JsonConvert.SerializeObject(value, serializeSettings);
            }

            public static T DeserializeObject<T>(string value)
            {
                return JsonConvert.DeserializeObject<T>(value, deserializeSettings);
            }
        }

        #endregion

        enum RestMethods
        {
            None,
            GetServerVersion,
            GetServerOptions,
            PutServerOptions,
            ValidateServerOptions,
            AddAccount,
            GetAccountsForSuper,
            PostAccount,
            GetAccount,
            PutAccount,
            DeleteAccount,
            GetUserz,
            GetUsers,
            PostUser,
            PutUser,
            DeleteUser,
            GetRole,
        }

        enum RestAuths
        {
            None,
            Admin,
            Super,
        }

        struct RestMethod
        {
            public RestMethod(RestMethods method, RestAuths auth)
            {
                Method = method;
                Auth = auth;
                UsersIndex = -1;
            }

            public RestMethod(RestMethods method, RestAuths auth, int usersIndex)
            {
                Method = method;
                Auth = auth;
                UsersIndex = usersIndex;
            }

            public readonly RestMethods Method;
            public readonly RestAuths Auth;
            public readonly int UsersIndex;
        }

        private RestMethod GetRestMethod(HttpMethods method)
        {
            switch (parser.BaseAction)
            {
                case BaseActions.Version:
                    if (method == HttpMethods.Get)
                        return new RestMethod(RestMethods.GetServerVersion, RestAuths.None);
                    break;


                case BaseActions.Role:
                    if (method == HttpMethods.Get)
                        return new RestMethod(RestMethods.GetRole, RestAuths.Admin);
                    break;


                case BaseActions.Options:
                    if (method == HttpMethods.Get)
                        return new RestMethod(RestMethods.GetServerOptions, RestAuths.Super);
                    if (method == HttpMethods.Put)
                        return new RestMethod(RestMethods.PutServerOptions, RestAuths.Super);
                    if (method == HttpMethods.Post)
                        return new RestMethod(RestMethods.ValidateServerOptions, RestAuths.Super);
                    break;


                case BaseActions.Accounts:
                    switch (parser.AccountAction)
                    {
                        case AccountActions.None:
                            if (parser.DomainName.IsInvalid)
                            {
                                if (method == HttpMethods.Get)
                                    return new RestMethod(RestMethods.GetAccountsForSuper, RestAuths.Super);
                                if (method == HttpMethods.Post)
                                    return new RestMethod(RestMethods.PostAccount, RestAuths.Super);
                            }
                            else
                            {
                                if (method == HttpMethods.Put)
                                    return new RestMethod(RestMethods.PutAccount, RestAuths.Admin);
                                if (method == HttpMethods.Get)
                                    return new RestMethod(RestMethods.GetAccount, RestAuths.Admin);
                                if (method == HttpMethods.Delete)
                                    return new RestMethod(RestMethods.DeleteAccount, RestAuths.Admin);
                            }
                            break;

                        case AccountActions.Userz:
                            if (parser.UsersId.IsInvalid)
                            {
                                if (method == HttpMethods.Get)
                                    return new RestMethod(RestMethods.GetUserz, RestAuths.Admin);
                            }
                            else
                            {
                                int usersIndex = userz.GetIndex(parser.UsersId.ToString());
                                if (usersIndex >= 0)
                                {
                                    if (parser.Username.IsInvalid)
                                    {
                                        if (method == HttpMethods.Get)
                                            return new RestMethod(RestMethods.GetUsers, RestAuths.Admin, usersIndex);
                                        if (method == HttpMethods.Post)
                                            return new RestMethod(RestMethods.PostUser, RestAuths.Admin, usersIndex);
                                    }
                                    else
                                    {
                                        if (method == HttpMethods.Put)
                                            return new RestMethod(RestMethods.PutUser, RestAuths.Admin, usersIndex);
                                        if (method == HttpMethods.Delete)
                                            return new RestMethod(RestMethods.DeleteUser, RestAuths.Admin, usersIndex);
                                    }
                                }
                            }
                            break;
                    }
                    break;
            }

            return new RestMethod();
        }

        private bool IsAuthorized(ByteArrayPart requestUri, RestAuths auth, IAccount account)
        {
            switch (auth)
            {
                case RestAuths.None:
                    return true;
                case RestAuths.Super:
                    return IsSuperAuthorized(requestUri);
                case RestAuths.Admin:
                    return IsAdminAuthorized(requestUri, account);
            }
            return false;
        }

        struct RestResult
        {
            public RestResult(Object result)
            {
                Result = Json.SerializeObject(result);
            }

            public RestResult(ConfigurationError[] errors)
            {
                var builder = new StringBuilder();

                builder.Append("[");
                for (int i = 0; i < errors.Length; i++)
                {
                    if (i > 0)
                        builder.Append(",");
                    builder.AppendFormat("{{\"message\": {0}}}", SerializeString(errors[i].Message));
                }
                builder.Append("]");

                Result = builder.ToString();
            }

            public static string SerializeString(string value)
            {
                return Json.SerializeObject(value);
            }

            public static RestResult CreateRaw(string raw)
            {
                return new RestResult(raw);
            }

            public readonly String Result;

            private RestResult(string raw)
            {
                Result = raw;
            }
        }

        private RestResult CallMethod(ArraySegment<byte> content, RestMethod method, IAccount account, IUsers users)
        {
            switch (method.Method)
            {
                case RestMethods.GetServerVersion:
                    return GetServerVersion();
                case RestMethods.GetRole:
                    return GetRole();
                case RestMethods.GetServerOptions:
                    return GetServerOptions();
                case RestMethods.PutServerOptions:
                    return PutServerOptions(content);
                case RestMethods.ValidateServerOptions:
                    return ValidateServerOptions(content);
                case RestMethods.GetAccountsForSuper:
                    return GetAccounts();
                case RestMethods.PostAccount:
                    return PostAccount(content);
                case RestMethods.GetAccount:
                    return GetAccount();
                case RestMethods.PutAccount:
                    return PutAccount(content);
                case RestMethods.DeleteAccount:
                    return DeleteAccount();
            }

            if (account == null)
                throw new Exception(@"Account not found");

            switch (method.Method)
            {
                case RestMethods.GetUserz:
                    return GetUserz(account);
                case RestMethods.GetUsers:
                    return GetUsers(account, users);
                case RestMethods.PostUser:
                    return PostUser(account, users, content);
                case RestMethods.PutUser:
                    return PutUser(account, users, content);
                case RestMethods.DeleteUser:
                    return DeleteUser(account, users);
            }

            throw new NotImplementedException(@"Restapi.CallMethod for " + method.Method.ToString() + " is not implemeted");
        }

        private void ExecuteInternal(BaseConnection c, HttpMessageReader httpReader, ArraySegment<byte> content)
        {
            var method = GetRestMethod(GetHttpMethod(httpReader.Method));

            if (method.Method == RestMethods.None)
            {
                SendResponse(c, StatusCodes.NotImplemented);
            }
            else
            {
                var domainName = (parser.DomainName.IsValid) ? parser.DomainName : parser.Authname;
                var account = accounts.GetAccount(domainName);

                if (IsAuthorized(httpReader.RequestUri, method.Auth, account) == false)
                {
                    SendResponse(c, StatusCodes.Forbidden);
                }
                else
                {
                    try
                    {
                        var result = CallMethod(content, method, account, (method.UsersIndex < 0) ? null : userz[method.UsersIndex]);
                        SendResponse(c, StatusCodes.OK, result.Result);
                    }
                    catch (Exception ex)
                    {
                        SendResponse(c, StatusCodes.NotAcceptable, "{ \"message\": \"" + ex.Message.ToString().Replace("\"", "\\\"") + "\" }");
                    }
                }
            }
        }

        private HttpMethods GetHttpMethod(HttpMethods httpMethod)
        {
            if (parser.Method == Methods.None)
            {
                return httpMethod;
            }
            else
            {
                if (parser.Method == Methods.Put)
                    return HttpMethods.Put;
                else if (parser.Method == Methods.Delete)
                    return HttpMethods.Delete;
            }

            return HttpMethods.None;
        }

        private RestResult GetServerVersion()
        {
            return new RestResult(version);
        }

        private RestResult GetRole()
        {
            return new RestResult((IsSuper() ? @"super" : @"admin") as object);
        }

        private RestResult GetServerOptions()
        {
            var configuration = SipServerConfigurationSection.GetSection();
            return new RestResult(configuration.ReadXml() as object);
        }

        private RestResult PutServerOptions(ArraySegment<byte> content)
        {
            var xml = Json.DeserializeObject<string>(GetContentString(content));

            var configuration = SipServerConfigurationSection.GetSection();

            var errors = Convert(configuration.Validate(xml));

            if (errors.Length == 0)
                configuration.WriteXml(xml);

            return new RestResult(errors);
        }

        private RestResult ValidateServerOptions(ArraySegment<byte> content)
        {
            var configuration = SipServerConfigurationSection.GetSection();

            return new RestResult(
                Convert(
                    configuration.Validate(
                        Json.DeserializeObject<string>(GetContentString(content)))));
        }

        struct ConfigurationError
        {
            public ConfigurationError(Exception ex)
            {
                Message = ex.Message;
            }

            public readonly string Message;
        }

        private ConfigurationError[] Convert(IList<Exception> errors)
        {
            var result = new ConfigurationError[errors.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = new ConfigurationError(errors[i]);
            return result;
        }

        private RestResult GetAccounts()
        {
            return new RestResult(accounts.GetAccounts());
        }

        private RestResult PostAccount(ArraySegment<byte> content)
        {
            return new RestResult(
                accounts.AddAccount(Json.DeserializeObject<Account>(GetContentString(content))));
        }

        private RestResult GetAccount()
        {
            return new RestResult(accounts.GetAccount(parser.DomainName));
        }

        private RestResult PutAccount(ArraySegment<byte> content)
        {
            var newOne = Json.DeserializeObject<Account>(GetContentString(content));
            var oldOne = accounts.GetAccount(parser.DomainName);

            if (oldOne == null)
                throw new Exception(@"Account not found");

            newOne.SetId(oldOne.Id);
            if (string.IsNullOrEmpty(newOne.Password))
                newOne.Password = oldOne.Password;
            if (string.IsNullOrEmpty(newOne.DomainName))
                newOne.DomainName = oldOne.DomainName;
            if (string.IsNullOrEmpty(newOne.Email))
                newOne.Email = oldOne.Email;

            accounts.UpdateAccount(newOne);

            return new RestResult();
        }

        private RestResult DeleteAccount()
        {
            accounts.Remove(parser.DomainName);

            return new RestResult();
        }

        private RestResult GetUserz(IAccount account)
        {
            return new RestResult(userz.ToArray());
        }

        private RestResult GetUsers(IAccount account, IUsers users)
        {
            int startIndex = (parser.StartIndex == int.MinValue) ? 0 : parser.StartIndex;
            int count = (parser.Count == int.MinValue) ? int.MaxValue : parser.Count;

            return new RestResult(
                users.GetUsers(account.Id, startIndex, count));
        }

        private RestResult PostUser(IAccount account, IUsers users, ArraySegment<byte> content)
        {
            users.Add(account.Id,
                Json.DeserializeObject<BaseUser>(GetContentString(content)));

            return new RestResult();
        }

        private RestResult PutUser(IAccount account, IUsers users, ArraySegment<byte> content)
        {
            users.Update(account.Id,
                Json.DeserializeObject<BaseUser>(GetContentString(content)));

            return new RestResult();
        }

        private RestResult DeleteUser(IAccount account, IUsers users)
        {
            users.Remove(account.Id, parser.Username.ToString());

            return new RestResult();
        }

        private bool IsSuper()
        {
            return parser.Authname.ToString() == @"administrator";
        }

        private bool IsSuperAuthorized(ByteArrayPart requestUri)
        {
            return IsSuper() && IsSignatureValid(requestUri, AdministratorPassword);
        }

        private bool IsAdminAuthorized(ByteArrayPart requestUri, IAccount account)
        {
            return ((account != null) && IsSignatureValid(requestUri, account.Password)) || IsSuperAuthorized(requestUri);
        }

        private static bool IsSignatureValid(ByteArrayPart requestUri, string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            hmac.Initialize();
            hmac.Key = Encoding.UTF8.GetBytes(password);

            var part = requestUri;
            var signature = hmac.ComputeHash(part.Bytes, part.Offset, part.Length - 5 - 32);

            for (int i = 0; i < 16; i++)
                if (HexEncoding.ParseHex2(parser.Signature, i * 2) != signature[i])
                    return false;

            return true;
        }

        private string GetContentString(ArraySegment<byte> content)
        {
            if (content.Count == 0)
                throw new Exception("HTTP content expected");

            return Encoding.UTF8.GetString(content.Array, content.Offset, content.Count);
        }

        private void SendResponse(BaseConnection c, StatusCodes statusCode)
        {
            SendResponse(c, statusCode, null);
        }

        private void SendResponse(BaseConnection c, StatusCodes statusCode, string json)
        {
            using (var writer = httpServer.GetHttpMessageWriter())
            {
                int length = (json == null) ? 0 : Encoding.UTF8.GetByteCount(json);

                writer.WriteStatusLine(statusCode);
                writer.WriteCacheControlNoCache();
                if (json != null)
                    writer.WriteContentType(ContentType.ApplicationJson);
                writer.WriteContentLength(length);
                writer.WriteCRLF();

                if (json != null)
                {
                    writer.ValidateCapacity(length);

                    Encoding.UTF8.GetBytes(json, 0, json.Length, writer.Buffer, writer.End);
                    writer.AddCount(length);
                }

                httpServer.SendResponse(c, writer);
            }
        }
    }
}

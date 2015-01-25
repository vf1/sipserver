using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Sip.Server.Accounts
{
    [Serializable]
    public class Account
        : IAccount
    {
        private string domainName;

        public const int InvalidId = -1;

        public Account()
        {
            Id = InvalidId;
        }

        public Account(IAccount other)
        {
            Id = other.Id;
            Email = other.Email;
            Password = other.Password;
            DomainName = other.DomainName;
        }

        public Account(int id, IAccount account)
        {
            Id = id;
            Email = account.Email;
            Password = account.Password;
            DomainName = account.DomainName;
        }

        [XmlIgnore()]
        public int Id { get; private set; }
        [XmlElement("email")]
        public string Email { get; set; }
        [XmlElement("password")]
        public string Password { get; set; }
        [XmlElement("domainName")]
        public string DomainName 
        {
            get { return domainName; }
            set { domainName = value.ToLower(); }
        }

        public void SetId(int id)
        {
            Id = id;
        }

        internal void Serialize(string fileName)
        {
            var path = Path.GetDirectoryName(fileName);
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);

            using (var stream = File.Open(fileName, FileMode.Create))
            {
                var serializer = new XmlSerializer(typeof(Account));
                serializer.Serialize(stream, this);
            }
        }

        internal static Account Deserialize(int id, string fileName)
        {
            using (var stream = File.Open(fileName, FileMode.Open))
            {
                var serializer = new XmlSerializer(typeof(Account));

                var account = (Account)serializer.Deserialize(stream);
                account.Id = id;

                return account;
            }
        }

        internal static void Delete(string fileName)
        {
            try
            {
                File.Delete(fileName);
            }
            catch
            {
            }
        }
    }
}

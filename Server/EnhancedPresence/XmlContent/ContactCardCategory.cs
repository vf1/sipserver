using System;
using System.Text;
using System.Xml;
using System.IO;

namespace EnhancedPresence
{
    public class ContactCardCategory :
        ICategoryValue
    {
        public string DisplayName { /*private */set; get; }
		public string Email { /*private */set; get; }

        public ContactCardCategory()
        {
        }

		/*
		public List<string> Emails { get; set; }

        public StateCategory()
        {
			this.Availability = 0;
        }

		public string Email1
		{
			set
			{
				SetEmail(0, value);
			}
		}

		public string Email1
		{
			set
			{
				SetEmail(0, value);
			}
		}

		private void SetEmail(int index, string email)
		{
			if (Emails == null)
				Emails = new List<string>();
			while (Emails.Count < index)
				Emails.Add(@"");
			Emails[index] = email;
		}
		 */

		public static ContactCardCategory Create(string displayName, string email)
        {
            ContactCardCategory card = new ContactCardCategory();

            card.DisplayName = displayName;
			card.Email = email;

            return card;
        }

        public static ContactCardCategory Parse(XmlReader reader)
        {
            ContactCardCategory card = new ContactCardCategory();

            while (reader.Read())
            {
                if (reader.Name == @"displayName")
                {
                    while (reader.Read() && reader.NodeType != XmlNodeType.Text) ;
                    card.DisplayName = reader.ReadContentAsString();
                }

				if (reader.Name == @"email")
				{
					while (reader.Read() && reader.NodeType != XmlNodeType.Text) ;
					card.Email = reader.ReadContentAsString();
				}

				if (reader.Name == @"contactCard" && reader.NodeType == XmlNodeType.EndElement)
                    break;
            }           

            return card;
        }

        public void Generate(XmlWriter writer)
        {
            writer.WriteStartElement(@"contactCard", @"http://schemas.microsoft.com/2006/09/sip/contactcard");

            writer.WriteStartElement(@"identity");

            writer.WriteStartElement(@"name");
            writer.WriteStartElement(@"displayName");
            writer.WriteString(this.DisplayName);
            writer.WriteEndElement();
            writer.WriteEndElement();

			if (String.IsNullOrEmpty(this.Email) == false)
			{
				writer.WriteStartElement(@"email");
				writer.WriteString(this.Email);
				writer.WriteEndElement();
			}

            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.Flush();
        }
    }
}

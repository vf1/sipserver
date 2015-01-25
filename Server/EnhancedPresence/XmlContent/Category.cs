using System;
using System.Text;
using System.Xml;
using System.IO;


namespace EnhancedPresence
{
    public enum CategoryExpireType
    {
        Static,
        Endpoint,
        User,
        Time
    }

    public class Category
    {
        public string Name { private set; get; }
        public uint? Instance;
        public string PublishTimeString;
        public ushort Container;
        public uint Version;
        public CategoryExpireType ExpireType;
        public string EndpointId;
        public int? Expires;

        private ICategoryValue value;

        public Category()
        {
			this.ExpireType = CategoryExpireType.Static;
        }

        public static Category Create(string name)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            Category category = new Category();

            category.Name = name;

            return category;
        }

        public static Category Parse(XmlReader reader)
        {
            string xmlElement = reader.Name;

            Category category = new Category();

            if(xmlElement == "category")
                category.Name = reader.GetAttribute("name");
            else if (xmlElement == "publication")
                category.Name = reader.GetAttribute("categoryName");

            category.InstanceString = reader.GetAttribute("instance");
			category.ContainerString = reader.GetAttribute("container");
			category.PublishTimeString = reader.GetAttribute("publishTime");
            category.VersionString = reader.GetAttribute("version");
            category.ExpireTypeString = reader.GetAttribute("expireType");
            category.EndpointId = reader.GetAttribute("endpointId");
            category.ExpiresString = reader.GetAttribute("expires");

            if (category.IsContactCardCategory())
                category.ContactCard = ContactCardCategory.Parse(reader);
            else if (category.IsStateCategory())
                category.State = StateCategory.Parse(reader);

            while (reader.Name != xmlElement && reader.NodeType == XmlNodeType.EndElement)
                reader.Read();

            return category;
        }

        public void Generate(XmlWriter writer)
        {
            writer.WriteStartElement(@"category");
            writer.WriteAttributeString(@"name", this.Name);

            if (this.InstanceString != null)
                writer.WriteAttributeString(@"instance", this.InstanceString);

            if (this.PublishTimeString != null)
                writer.WriteAttributeString(@"publishTime", this.PublishTimeString);

            if (this.ContainerString != null)
                writer.WriteAttributeString(@"container", this.ContainerString);

            if (this.VersionString != null)
                writer.WriteAttributeString(@"version", this.VersionString);

            if (this.ExpireTypeString != null)
                writer.WriteAttributeString(@"expireType", this.ExpireTypeString);

            //if (this.EndpointId != null)
            //    writer.WriteAttributeString(@"endpointId", this.EndpointId);

            if (this.ExpiresString != null)
                writer.WriteAttributeString(@"expires", this.ExpiresString);

            if (value != null)
                value.Generate(writer);

            writer.WriteEndElement();
        }

        public string InstanceString
        {
            set
            {
                uint instance;
                this.Instance = uint.TryParse(value, out instance) ? instance : (uint?)null;
            }
            get
            {
                return (this.Instance != null)
                    ? this.Instance.ToString() : null;
            }
        }

        public string ContainerString
        {
            set
            {
                ushort container;
                this.Container = ushort.TryParse(value, out container) ? container : (ushort)0;
            }
            get
            {
                return this.Container.ToString();
            }
        }

        public string VersionString
        {
            set 
            {
                uint version;
                this.Version = uint.TryParse(value, out version) ? version : 0;
            }
            get
            {
                return this.Version.ToString();
            }
        }

        public string ExpireTypeString
        {
            set
            {
                if (value == "endpoint")
                    this.ExpireType = CategoryExpireType.Endpoint;
                else if (value == "user")
                    this.ExpireType = CategoryExpireType.User;
                else if (value == "time")
                    this.ExpireType = CategoryExpireType.Time;
                else
					this.ExpireType = CategoryExpireType.Static;
            }
            get
            {
				switch(this.ExpireType)
				{
					case CategoryExpireType.Endpoint:
						return "endpoint";
					case CategoryExpireType.Static:
						return "static";
					case CategoryExpireType.Time:
						return "time";
					case CategoryExpireType.User:
						return "user";
					default:
						throw new NotImplementedException();
				}
            }
        }

        public string ExpiresString
        {
            set
            {
                int expires;
                this.Expires = int.TryParse(value, out expires) ? expires : (int?)null;
            }
            get
            {
                return (this.Expires != null)
                    ? this.Expires.ToString() : null;
            }
        }

        public bool IsContactCardCategory()
        {
            return this.Name == "contactCard";
        }

        public bool IsStateCategory()
        {
            return this.Name == "state";
        }

		public bool IsUserPropertiesCategory()
		{
			return this.Name == "userProperties";
		}

		public ContactCardCategory ContactCard
        {
            get
            {
                return IsContactCardCategory()
                    ? this.value as ContactCardCategory : null;
            }
            set
            {
				this.Name = @"contactCard";
				this.value = value;
            }
        }

        public StateCategory State
        {
            get
            {
                return IsStateCategory()
                    ? this.value as StateCategory : null;
            }
            set
            {
				this.Name = @"state";
                this.value = value;
            }
        }

		public UserPropertiesCategory UserProperties
		{
			get
			{
				return IsUserPropertiesCategory()
					? this.value as UserPropertiesCategory : null;
			}
			set
			{
				this.Name = @"userProperties";
				this.value = value;
			}
		}

		public ICategoryValue CategoryValue
		{
			get
			{
				return this.value;
			}
			set
			{
				this.value = value;
			}
		}

		public bool IsSame(Category category)
		{
			return this.Name == category.Name
				&& this.Instance == category.Instance
				&& this.Container == category.Container;
		}
	}
}

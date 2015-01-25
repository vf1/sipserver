using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace EnhancedPresence
{
    public class Categories :
		OutContentBase
    {
        public string Uri { private set; get; }
        public List<Category> Items { private set; get; }

        protected Categories()
        {
            Items = new List<Category>();
        }

		public static Categories Create(string uri)
        {
			Categories categories = new Categories();

            categories.Uri = uri;

            return categories;
        }

        public static Categories Parse(XmlReader reader)
        {
			Categories categories = new Categories();

            while (reader.Read())
            {
                if ((reader.Name == "categories" || reader.Name == "publications")
                    && reader.NodeType == XmlNodeType.Element)
                {
                    categories.Uri = reader.GetAttribute("uri");
                }

                if ((reader.Name == "category" || reader.Name == "publication")
                    && reader.NodeType == XmlNodeType.Element)
                {
                    categories.Items.Add(
                        Category.Parse(reader)
                        );
                }
            }

            return categories;
        }

		public static string InContentType
		{
			get { return @"application"; }
		}

		public static string InContentSubtype
		{
			get { return @"msrtc-category-publish+xml"; }
		}

		public override void Generate(XmlWriter writer)
        {
            writer.WriteStartElement(@"categories", @"http://schemas.microsoft.com/2006/09/sip/categories");
            writer.WriteAttributeString(@"uri", this.Uri);

            foreach (Category category in this.Items)
                category.Generate(writer);

            writer.WriteEndElement();
        }

		public override string OutContentType
        {
            get { return @"application"; }
        }

		public override string OutContentSubtype
		{
			get { return @"msrtc-event-categories+xml"; }
		}
		/*
		public Category FindCategory(string name, uint? instance, ushort container)
		{
			foreach (Category category in this.Items)
				if (category.Name == name 
					&& category.Instance == instance 
					&& category.Container == container)
				{
					return category;
				}

			return null;
		}

		public Category FindCategory(Category searchTerms)
		{
			return this.FindCategory(searchTerms.Name, searchTerms.Instance, searchTerms.Container);
		}
		*/
		public bool IsEmpty()
		{
			return this.Items.Count == 0;
		}
		/*
		public bool RemoveCategories(CategoryExpireType expireType)
		{
			int count = 0;

			for (int i = this.Items.Count - 1; i >= 0; i--)
			{
				Category category = this.Items[i];

				if (category.ExpireType == expireType)
				{
					this.Items.Remove(category);
					count++;
				}
			}

			return count > 0;
		}

		public bool RemoveEndpointCategories(string endpointId)
		{
			int count = 0;

			for (int i = this.Items.Count - 1; i >= 0; i--)
			{
				Category category = this.Items[i];

				if (category.ExpireType == CategoryExpireType.Endpoint && category.EndpointId == endpointId)
				{
					this.Items.Remove(category);
					count++;
				}
			}

			return count > 0;
		}
		*/
	}
}

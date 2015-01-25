using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml;
using System.IO;

namespace EnhancedPresence
{
    public enum EnhancedPresenceSchema
    {
        BatchSubscribe = 0,
        Categories = 1,
        NoSchema
    }

    public class Schemas
    {
        public XmlSchemaSet[] schemas;

        public Schemas()
        {
            this.schemas = new XmlSchemaSet[2];

            {
                XmlSchemaSet schemaSet = new XmlSchemaSet();
                schemaSet.Add(LoadSchema("BatchSubscribeRequest.xsd"));
                schemaSet.Add(LoadSchema("categoryList.xsd"));
                this[EnhancedPresenceSchema.BatchSubscribe] = schemaSet;
            }

            {
                XmlSchemaSet schemaSet = new XmlSchemaSet();
                schemaSet.Add(LoadSchema("Categories.xsd"));
                schemaSet.Add(LoadSchema("RichPresenceCommon.xsd"));
                this[EnhancedPresenceSchema.Categories] = schemaSet;
            }
        }

        public XmlSchemaSet this[EnhancedPresenceSchema id]
        {
            get
            {
                return this.schemas[(int)id];
            }
            private set
            {
                this.schemas[(int)id] = value;
            }
        }

		private XmlSchema LoadSchema(string location)
		{
			string[] resources = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();

			foreach (string resource in resources)
			{
				if (resource.EndsWith(location))
					using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(@"Sip.Server.EnhancedPresence.Schemas." + location))
						return XmlSchema.Read(stream, null);
			}

			throw new Exception("Schema " + location + " can not be loaded.");
		}
    }
}

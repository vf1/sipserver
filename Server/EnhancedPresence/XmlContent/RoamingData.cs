using System;
using System.Xml;
using System.Text;

namespace EnhancedPresence
{
    public class RoamingData :
        OutContentBase
    {
        private Categories categories;

        protected RoamingData()
        {
        }

        public static RoamingData Create(Categories categories)
        {
            RoamingData roamingData = new RoamingData();

            roamingData.categories = categories;

            return roamingData;
        }

		public override void Generate(XmlWriter writer)
        {
            writer.WriteStartElement(@"roamingData", @"http://schemas.microsoft.com/2006/09/sip/roaming-self");

			this.categories.Generate(writer);

            writer.WriteEndElement();

            writer.Flush();
        }

		public override string OutContentType 
        {
            get { return @"application"; } 
        }

		public override string OutContentSubtype
        {
            get { return @"vnd-microsoft-roaming-self+xml"; } 
        }
    }
}

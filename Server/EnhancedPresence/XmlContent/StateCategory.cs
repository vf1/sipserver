using System;
using System.Text;
using System.Xml;
using System.IO;

namespace EnhancedPresence
{
    public class StateCategory :
        ICategoryValue
    {
        public int Availability { /*private */set; get; }

        public StateCategory()
        {
        }

        public static StateCategory Create(int availability)
        {
            StateCategory state = new StateCategory();

            state.Availability = availability;

            return state;
        }

        public static StateCategory Parse(XmlReader reader)
        {
            StateCategory state = new StateCategory();

            while (reader.Read())
            {
                if (reader.Name == "availability")
                {
                    while (reader.Read() && reader.NodeType != XmlNodeType.Text) ;
                    state.Availability = reader.ReadContentAsInt();
                }

                if (reader.Name == "state" && reader.NodeType == XmlNodeType.EndElement)
                    break;
            }

            return state;
        }

        public void Generate(XmlWriter writer)
        {
            writer.WriteStartElement(@"state", @"http://schemas.microsoft.com/2006/09/sip/state");
            writer.WriteAttributeString(@"xsi", @"type", @"http://www.w3.org/2001/XMLSchema-instance", @"aggregateState");

            writer.WriteStartElement(@"availability");

            writer.WriteValue(this.Availability);

            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.Flush();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml;
using System.IO;

namespace EnhancedPresence
{
    public enum BatchSubscribeAction
    {
        Subscribe,
        Unsubscribe
    }

    public class BatchSubscribe
    {
        public BatchSubscribeAction Action { private set; get; }
        public List<string> Resources { private set; get; }
        public List<string> Сategories { private set; get; }

        protected BatchSubscribe()
        {
            Resources = new List<string>();
            Сategories = new List<string>();
        }

        public static BatchSubscribe Parse(XmlReader reader)
        {
            BatchSubscribe request = new BatchSubscribe();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "action")
                    {
                        string name = reader.GetAttribute("name");
                        if (name == "subscribe")
                            request.Action = BatchSubscribeAction.Subscribe;
                        else if (name == "unsubscribe")
                            request.Action = BatchSubscribeAction.Unsubscribe;
                    }

                    if (reader.Name == "resource")
                        request.Resources.Add(reader.GetAttribute("uri"));

                    if (reader.Name == "category")
                        request.Сategories.Add(reader.GetAttribute("name"));
                }
            }

            return request;
        }

		public static string InContentType
		{
			get { return @"application"; }
		}

		public static string InContentSubtype
		{
			get { return @"msrtc-adrl-categorylist+xml"; }
		}
    }
}

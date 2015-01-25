using System;
using System.Xml;

namespace EnhancedPresence
{
    public interface ICategoryValue
    {
        void Generate(XmlWriter writer);
    }
}

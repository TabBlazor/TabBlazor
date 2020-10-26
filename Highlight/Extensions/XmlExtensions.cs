using System;
using System.Xml.Linq;

namespace Highlight.Extensions
{
    internal static class XmlExtensions
    {
        public static string GetAttributeValue(this XElement element, XName name)
        {
            if (element == null) {
                throw new ArgumentNullException("element");
            }

            var attribute = element.Attribute(name);
            if (attribute == null) {
                return null;
            }

            return attribute.Value;
        }
    }
}
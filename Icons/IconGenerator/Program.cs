using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace IconGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            GenerateTablerIcons();
        }

        private static void GenerateTablerIcons()
        {
            var directory = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Parent.Parent;
            var svgPath = Path.Combine(directory.FullName, @"Tabler\tabler-sprite.svg");

            var icons = new List<string>();
            StringBuilder fileOutput = new StringBuilder();

            XElement svg = XDocument.Load(svgPath).Root;
            RemoveAllNamespaces(svg);
            var iconElements = svg.Descendants().Where(e => e.Name.LocalName == "symbol");

            foreach (var iconElement in iconElements)
            {
                var elements = iconElement.Elements();
                var elementsString = string.Join("", elements.Select(e => e));
                elementsString = elementsString.Replace(@"""", "'");

                var iconName = iconElement.Attributes().First(e => e.Name == "id").Value;
                iconName = iconName.Replace("tabler-", "");
                iconName = iconName.Replace("-", "_");
                if (char.IsDigit(iconName.ToCharArray().First()))
                {
                    iconName = "_" + iconName;
                }
                else
                {
                    iconName = iconName.FirstCharacterToUpperCase();
                }

                fileOutput.AppendLine($"public static string {iconName} {"{"} get => @\"{elementsString}\"; {"}"} ");
            }

            File.WriteAllText(Path.Combine(directory.FullName, "Generated", "TablerIcons.txt"), fileOutput.ToString());

        }

        private static void RemoveAllNamespaces(XElement element)
        {
            element.Name = element.Name.LocalName;

            foreach (var node in element.DescendantNodes())
            {
                var xElement = node as XElement;
                if (xElement != null)
                {
                    RemoveAllNamespaces(xElement);
                }
            }
        }

    }
}

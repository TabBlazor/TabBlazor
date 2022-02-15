using IconGenerator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace IconGenerator
{
    public static class Utilities
    {
        public static string GetStaticPropertyString(string iconName, string elementsString)
        {
            return $"public static string {iconName} => @\"{elementsString}\";";
        }

        public static string ExtractIconElements(string svgContent)
        {
            var svg = XElement.Parse(svgContent);
            svg.RemoveAllNamespaces();
            var elements = svg.Elements();
            var elementsString = string.Join("", elements.Select(e => e));
            elementsString = elementsString.Replace(@"""", "'");
            elementsString = elementsString.Replace(Environment.NewLine, "");

            return elementsString;
        }


        public static void GenerateIconsFile(string fileName, IEnumerable<GeneratedIcon> icons)
        {
            var directory = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Parent.Parent;
            var fileOutput = new StringBuilder();
            foreach (var icon in icons)
            {
                fileOutput.AppendLine(icon.DotNetProperty);
            }

            File.WriteAllText(Path.Combine(directory.FullName, "Generated", fileName), fileOutput.ToString());


        }
    }
}

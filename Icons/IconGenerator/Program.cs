using IconGenerator.MaterialDesign;
using IconGenerator.Tabler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IconGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //GenerateTablerIcons();
             await GenerateMaterialDesignIcons();
            //await GenerateTablerIcons();
        }

        private static async Task GenerateTablerIcons()
        {
            var icons = await TablerGenerator.GenerateIcons();
        }

        private static async Task GenerateMaterialDesignIcons()
        {
            var icons = await MaterialDesignGenerator.GenerateIcons();
        }

        private static void GenerateMaterialIcons()
        {
            var directory = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Parent.Parent;
            var iconsPath = Path.Combine(directory.FullName, @"Material\Icons");
   
        }

        private static void GenerateTablerIconsOLD()
        {
            var directory = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Parent.Parent;
            var svgPath = Path.Combine(directory.FullName, @"Tabler\tabler-sprite.svg");

            var icons = new List<string>();
            StringBuilder fileOutput = new StringBuilder();

            XElement svg = XDocument.Load(svgPath).Root;
            svg.RemoveAllNamespaces();
            //RemoveAllNamespaces(svg);
            var iconElements = svg.Descendants().Where(e => e.Name.LocalName == "symbol");

            foreach (var iconElement in iconElements)
            {
                var elements = iconElement.Elements();
                var elementsString = string.Join("", elements.Select(e => e));
                elementsString = elementsString.Replace(@"""", "'");

                var iconName = iconElement.Attributes().First(e => e.Name == "id").Value;
                iconName = iconName.Replace("tabler-", "");
                iconName = iconName.GetIconName();
                fileOutput.AppendLine($"public static string {iconName} => @\"{elementsString}\";");
            }

            File.WriteAllText(Path.Combine(directory.FullName, "Generated", "TablerIcons.txt"), fileOutput.ToString());

        }



    }
}

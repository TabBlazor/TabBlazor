using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IconGenerator.Material
{
    public static class MaterialIconGenerator
    {

        public static void Generate(string basePath, string iconType)
        {
            var dir = new DirectoryInfo(basePath);
            var fileOutput = new StringBuilder();
            foreach (var categoryDir in dir.GetDirectories())
            {
                Console.WriteLine($"Catagory: {categoryDir.Name}");
                foreach (var iconNameDir in categoryDir.GetDirectories())
                {
                    Console.WriteLine($"Icon: {iconNameDir.Name}");


                    var iconDir = iconNameDir.GetDirectories().FirstOrDefault(e => e.Name == iconType);
                    var iconFile = iconDir?.GetFiles().FirstOrDefault(e => e.Name == "24px.svg");
                    var iconName = iconNameDir.Name.GetIconName();
                    if (iconFile == null) { continue; }

                    var svgContent = File.ReadAllText(iconFile.FullName);
                    XElement svg = XElement.Parse(svgContent);
                    svg.RemoveAllNamespaces();
                    var elements = svg.Elements();
                    CleanBorder(elements);
                    //if (elements.Count() == 1)
                    //{
                    //    if (elements.Elements().Count() >= 2)
                    //    {
                    //        var el = elements.First();
                    //        el.Remove();
                    //    }
                    //}

                    //else if (elements.Count() >= 2)
                    //{
                    //    var el = elements.First();
                    //    el.Remove();
                    //var d = el.Attribute("d");
                    //if (d?.Value == "M0 0h24v24H0V0z" || el.ToString() == "<rect fill=\"none\" height=\"24\" width=\"24\" />")
                    //{
                    //    el.Remove();
                    //}
                    //else
                    //{
                    //    var kalle = "Anka";
                    //}

                    //}

                    var elementsString = string.Join("", elements.Select(e => e));
                    elementsString = elementsString.Replace(@"""", "'");
                    elementsString = elementsString.Replace(System.Environment.NewLine, "");
                    fileOutput.AppendLine(Helpers.GetStaticPropertyString(iconName, elementsString));

                };

            }

            File.WriteAllText(Path.Combine(dir.Parent.Parent.FullName, "Generated", $"{iconType}.txt"), fileOutput.ToString());


        }

        private static void CleanBorder(IEnumerable<XElement> elements)
        {
            if (elements == null) { return; }
            foreach (var el in elements)
            {
                var d = el.Attribute("d");

                var dValue = d?.Value ?? "";

                var fillValue = el.Attribute("fill")?.Value ?? "";

                //if (dValue.Contains("0h24v24H0V0z") ||
                //    dValue.Contains("0h24v24H0z") ||
                //    dValue.Contains("0h24v24H0V0z") ||
                //    dValue.Contains("24H0V0h24v24z") || //0v24H0V0h24z
                //    el.ToString().StartsWith("<rect fill=\"none\" height=\"24\" width=\"24\""))
                if (fillValue == "none")
                {
                    el.Remove();
                    if (el.Parent != null && !el.Parent.Elements().Any())
                    {
                        el.Parent.Remove();
                    }

                }
                else
                {
                    CleanBorder(el.Elements());
                   
                }

            }
            
        }



    }


}

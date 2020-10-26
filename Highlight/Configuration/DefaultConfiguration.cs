using System.Xml.Linq;

namespace Highlight.Configuration
{
    public class DefaultConfiguration : XmlConfiguration
    {
        public DefaultConfiguration()
        {
            //XmlDocument = XDocument.Parse(Resources.DefaultDefinitions);
            XmlDocument = XDocument.Load(@"C:\SourceCodeGitHub\Blazor-Tabler\Highlight\Resources\DefaultDefinitions.xml");
        }
    }
}
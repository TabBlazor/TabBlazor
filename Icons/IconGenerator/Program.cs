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
           //await TablerGenerator.GenerateFlags();
            await TablerGenerator.GenerateIcons();
            //await MaterialDesignGenerator.GenerateIcons();
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public class Country
    {
        public Country(string name, string alpha2, string alpha3, int numeric)
        {
            Name = name; 
            Alpha2 = alpha2; 
            Alpha3 = alpha3;
            Numeric = numeric;
        }

        public string Name { get; set; }
        public string Alpha2 { get; set; }
        public string Alpha3 { get; set; }
        public int Numeric { get; set; }

    }
}

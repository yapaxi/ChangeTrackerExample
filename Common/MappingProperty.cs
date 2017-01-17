using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class MappingProperty
    {
        public static readonly MappingProperty[] Childless = new MappingProperty[0];

        public string ShortName { get; set; }
        public string PathName { get; set; }
        public string ClrType { get; set; }
        public int? Size { get; set; }
        public MappingProperty[] Children { get; set; }

        public static string ConcatPathName(string parent, string name)
        {
            return string.IsNullOrWhiteSpace(parent) ? name : $"{parent}.{name}";
        }
    }
}

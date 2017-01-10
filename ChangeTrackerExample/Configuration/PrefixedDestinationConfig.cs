using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace ChangeTrackerExample.Configuration
{
    public class PrefixedDestinationConfig
    {
        public string Prefix { get; }
        public string RootNamespace { get; }

        internal PrefixedDestinationConfig(string rootNamespace, string prefix)
        {
            EnsureNameIsValid(prefix);
            
            this.Prefix = prefix;
            this.RootNamespace = rootNamespace;
        }
        
        private static void EnsureNameIsValid(string exchangePrefix)
        {
            if (string.IsNullOrWhiteSpace(exchangePrefix))
            {
                throw new FormatException($"Emtpty exchange prefix name");
            }

            if (!exchangePrefix.All(e => (char.IsLetter(e) && char.IsLower(e)) || char.IsDigit(e) || e == '.' || e == '-'))
            {
                throw new FormatException($"Invalid exchange prefix name: {exchangePrefix}. Allowed symbols: lower letters, digits, '.', '-'");
            }
        }

        public override string ToString() => $"{RootNamespace}.{Prefix}";
    }
}

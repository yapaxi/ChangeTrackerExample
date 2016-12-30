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
        public bool ComplexObjectsAllowed { get; }
        public string Prefix { get; }
        public string RootNamespace { get; }

        internal PrefixedDestinationConfig(string rootNamespace, string prefix, bool complexObjectsAllowed)
        {
            EnsureNameIsValid(prefix);

            this.ComplexObjectsAllowed = complexObjectsAllowed;
            this.Prefix = prefix;
            this.RootNamespace = rootNamespace;
        }

        public PrefixedDestinationConfig AllowComplexObjects()
        {
            return new PrefixedDestinationConfig(RootNamespace, Prefix, complexObjectsAllowed: true);
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

﻿using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class DestinationConfig
    {
        private readonly string _rootNamespace;

        internal DestinationConfig(string rootNamespace)
        {
            if (string.IsNullOrWhiteSpace(rootNamespace))
            {
                throw new ArgumentException($"{nameof(rootNamespace)} cannot be null or empty", nameof(rootNamespace));
            }

            _rootNamespace = rootNamespace;
        }
        
        public PrefixedDestinationConfig Prefixed(string prefix)
        {
            return new PrefixedDestinationConfig(_rootNamespace, prefix);
        }

        public DestinationConfig ComplexObjectsAllowed()
        {
            return new DestinationConfig(_rootNamespace);
        }
    }
}

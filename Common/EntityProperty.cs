﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class EntityProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int? Size { get; set; }
        public IReadOnlyCollection<EntityProperty> Children { get; set; }
    }
}
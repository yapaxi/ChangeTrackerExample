﻿using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Converters
{
    interface IConverter
    {
        MappingSchema Schema { get; }

        KeyValuePair<string, object>[] Convert(byte[] data);
    }
}

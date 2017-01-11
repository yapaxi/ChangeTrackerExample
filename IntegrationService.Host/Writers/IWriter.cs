﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Writers
{
    public interface IWriter<TData>
    {
        void Write(TData rootFlattenRepresentation);
    }
}
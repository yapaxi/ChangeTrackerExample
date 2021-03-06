﻿using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class RegisteredEntity<TSource>
        where TSource : class, IEntity
    {
        internal RegisteredEntity()
        {

        }

        public ContextEntity<TSourceContext, TSource> FromContext<TSourceContext>()
            where TSourceContext : IEntityContext
        {
            return new ContextEntity<TSourceContext, TSource>();
        }
    }
}

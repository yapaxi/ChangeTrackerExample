﻿using ChangeTrackerExample.Configuration;
using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class MappedContextEntity<TSourceContext, TSource, TTarget> : IBoundedMappedEntity
        where TSourceContext : IEntityContext
        where TSource : class, IEntity
        where TTarget : class
    {
        private readonly MD5 _md5;

        public Expression<Func<TSource, TTarget>> Mapper { get; }

        public string Name { get; }

        internal MappedContextEntity(string name, Expression<Func<TSource, TTarget>> mapper)
        {
            _md5 = MD5.Create();
            Mapper = mapper;
            TargetType = typeof(TTarget);
            var properties = GetProperties(typeof(TTarget));
            var checksum = GetMD5(JsonConvert.SerializeObject(properties, Formatting.None));
            MappingSchema = new MappingSchema(properties, checksum, DateTime.UtcNow);
            Name = name;
        }

        public async Task<object> GetAndMapByIdAsync(IEntityContext context, int id)
        {
            var o = await context.Get<TSource>().Where(e => e.Id == id).Select(Mapper).FirstOrDefaultAsync();
            return o;
        }

        public async Task<IReadOnlyCollection<object>> GetAndMapByRangeAsync(IEntityContext context, int fromId, int toId)
        {
            var o = await context.Get<TSource>().Where(e => e.Id >= fromId && e.Id <= toId).Select(Mapper).ToListAsync();
            return o;
        }
        
        public object GetAndMapById(IEntityContext context, int id)
        {
            return context.Get<TSource>().Where(e => e.Id == id).Select(Mapper).FirstOrDefault();
        }

        public IReadOnlyCollection<object> GetAndMapByRange(IEntityContext context, int fromId, int toId)
        {
            return context.Get<TSource>().Where(e => e.Id >= fromId && e.Id <= toId).Select(Mapper).ToList();
        }

        private long GetMD5(string str)
        {
            var array = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, array, 0, array.Length);
            return BitConverter.ToInt64(_md5.ComputeHash(array), 0);
        }

        private IReadOnlyCollection<MappingProperty> GetProperties(Type t)
        {
            var lst = new List<MappingProperty>();

            foreach (var p in t.GetProperties())
            {
                var attrs = p.GetCustomAttributes(false);
                if (attrs.Any(e => e is JsonIgnoreAttribute || e is NotMappedAttribute))
                {
                    continue;
                }
                
                if (p.PropertyType.IsPrimitive || 
                    p.PropertyType == typeof(string) || 
                    p.PropertyType == typeof(Guid))
                {
                    lst.Add(new MappingProperty()
                    {
                        Name = p.Name,
                        Type = p.PropertyType.Name,
                        Size = attrs.OfType<MaxLengthAttribute>().FirstOrDefault()?.Length,
                        Children = new MappingProperty[0]
                    });
                }
                else
                {
                    var complex = new MappingProperty();
                    complex.Name = p.Name;
                    complex.Type = p.PropertyType.Name;
                    complex.Children = GetProperties(p.PropertyType);
                }
            }

            return lst;
        }

        #region IBoundedMappedEntity
        
        public Type ContextType => typeof(TSourceContext);
        public Type SourceType => typeof(TSource);
        public Type TargetType { get; }

        public MappingSchema MappingSchema { get; }

        #endregion
    }

    public interface IBoundedMappedEntity
    {
        string Name { get; }
        Type SourceType { get; }
        Type TargetType { get; }
        MappingSchema MappingSchema { get; }
        Type ContextType { get; }
        Task<object> GetAndMapByIdAsync(IEntityContext context, int id);
        Task<IReadOnlyCollection<object>> GetAndMapByRangeAsync(IEntityContext context, int fromId, int toId);

        object GetAndMapById(IEntityContext context, int id);
        IReadOnlyCollection<object> GetAndMapByRange(IEntityContext context, int fromId, int toId);
    }
}

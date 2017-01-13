using ChangeTrackerExample.Configuration;
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
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class MappedEntityRoot<TSourceContext, TSource, TTarget> : IBoundedMappedEntity
        where TSourceContext : IEntityContext
        where TSource : class, IEntity
        where TTarget : class
    {
        private readonly MD5 _md5;

        public Expression<Func<TSource, TTarget>> Mapper { get; }

        public IReadOnlyDictionary<string, ParentChildConfiguration> Children { get; }

        public string ShortName { get; }

        internal MappedEntityRoot(
            string name,
            Expression<Func<TSource, TTarget>> mapper,
            IReadOnlyDictionary<string, ParentChildConfiguration> parentChildConfig)
        {
            _md5 = MD5.Create();
            Mapper = mapper;
            TargetType = typeof(TTarget);
            var properties = GetProperties(typeof(TTarget));
            var checksum = GetMD5(JsonConvert.SerializeObject(properties, Formatting.None));
            MappingSchema = new MappingSchema(properties, checksum, DateTime.UtcNow);
            ShortName = name;
            Children = parentChildConfig;
        }

        public async Task<object> GetAndMapByIdAsync(IEntityContext context, int id)
        {
            var o = await context.ReadonlyGet<TSource>().Where(e => e.Id == id).Select(Mapper).FirstOrDefaultAsync();
            return o;
        }

        public async Task<IReadOnlyCollection<object>> GetAndMapByRangeAsync(IEntityContext context, int fromId, int toId)
        {
            var o = await context.ReadonlyGet<TSource>().Where(e => e.Id >= fromId && e.Id <= toId).Select(Mapper).ToListAsync();
            return o;
        }
        
        public object GetAndMapById(IEntityContext context, int id)
        {
            return context.ReadonlyGet<TSource>().Where(e => e.Id == id).Select(Mapper).FirstOrDefault();
        }

        public IReadOnlyCollection<object> GetAndMapByRange(IEntityContext context, int fromId, int toId)
        {
            return context.ReadonlyGet<TSource>().Where(e => e.Id >= fromId && e.Id <= toId).Select(Mapper).ToList();
        }

        public EntityRange[] GetEntityRanges(IEntityContext context, int rangeLength)
        {
            return (
                from q in context.ReadonlyGet<TSource>()
                group q by q.Id / rangeLength into grp
                select new { Key = grp.Key, Min = grp.Min(q => q.Id), Max = grp.Max(q => q.Id) }
            )
            .ToArray()
            .Select(e => new EntityRange() { MinId = e.Min, MaxId = e.Max })
            .ToArray();
        }

        private long GetMD5(string str)
        {
            var array = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, array, 0, array.Length);
            return BitConverter.ToInt64(_md5.ComputeHash(array), 0);
        }

        private MappingProperty[] GetProperties(Type t, string parentName = null)
        {
            var lst = new List<MappingProperty>();

            foreach (var p in t.GetProperties())
            {
                var fullPath = string.IsNullOrWhiteSpace(parentName) 
                                    ? p.Name 
                                    : MappingProperty.ConcatPathName(parentName, p.Name);

                var attrs = p.GetCustomAttributes(false);
                if (attrs.Any(e => e is JsonIgnoreAttribute || e is NotMappedAttribute))
                {
                    continue;
                }

                string typeName;
                if (IsSimpleType(p.PropertyType, out typeName))
                {
                    lst.Add(new MappingProperty()
                    {
                        ShortName = p.Name,
                        PathName = fullPath,
                        ClrType = typeName,
                        Size = attrs.OfType<MaxLengthAttribute>().FirstOrDefault()?.Length,
                        Children = new MappingProperty[0]
                    });
                }
                else
                {
                    var complex = new MappingProperty();
                    complex.ShortName = p.Name;
                    complex.PathName = fullPath;
                    if (p.PropertyType.IsGenericType)
                    {
                        var openGenericType = p.PropertyType.GetGenericTypeDefinition();
                        if (openGenericType == typeof(ICollection<>) || openGenericType == typeof(IEnumerable<>))
                        {
                            var collectionElementType = p.PropertyType.GetGenericArguments()[0];
                            complex.Children = GetProperties(collectionElementType, fullPath);
                            lst.Add(complex);
                        }
                        else if (p.PropertyType.GetCustomAttributes(false).OfType<CompilerGeneratedAttribute>().Any())
                        {
                            complex.Children = GetProperties(p.PropertyType, fullPath);
                            lst.Add(complex);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    else if (p.PropertyType.IsArray)
                    {
                        var arrayElementType = p.PropertyType.GetElementType();
                        complex.Children = GetProperties(arrayElementType, p.Name);
                        lst.Add(complex);
                    }
                    else
                    {
                        Console.WriteLine($"Ignoring {p.Name}");
                    }
                }
            }

            return lst.ToArray();
        }

        private static bool IsSimpleType(Type t, out string name)
        {
            if (!t.IsGenericType)
            {
                name = t.FullName;
                return t.IsPrimitive || t == typeof(string) || t == typeof(Guid);
            }
            else if (t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                name = t.FullName;
                string name2;
                return IsSimpleType(t.GetGenericArguments()[0], out name2);
            }
            else
            {
                name = null;
                return false;
            }
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
        string ShortName { get; }
        Type SourceType { get; }
        Type TargetType { get; }
        MappingSchema MappingSchema { get; }
        Type ContextType { get; }
        Task<object> GetAndMapByIdAsync(IEntityContext context, int id);
        Task<IReadOnlyCollection<object>> GetAndMapByRangeAsync(IEntityContext context, int fromId, int toId);
        IReadOnlyDictionary<string, ParentChildConfiguration> Children { get; }
        object GetAndMapById(IEntityContext context, int id);
        IReadOnlyCollection<object> GetAndMapByRange(IEntityContext context, int fromId, int toId);
        EntityRange[] GetEntityRanges(IEntityContext context, int rangeLength);
    }
}

using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class MappedContextEntity<TSourceContext, TSource, TTarget> : IBoundedMappedEntity
        where TSourceContext : IEntityContext
        where TSource : class, IEntity
        where TTarget : class
    {
        public Expression<Func<TSource, TTarget>> Mapper { get; }

        internal MappedContextEntity(Expression<Func<TSource, TTarget>> mapper)
        {
            Mapper = mapper;
            TargetType = typeof(TTarget);
            TargetTypeSchema = GetTypeSchema(typeof(TTarget));
            SerializedTargetTypeSchema = JsonConvert.SerializeObject(TargetTypeSchema);
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

        private IReadOnlyCollection<EntityProperty> GetTypeSchema(Type t)
        {
            var lst = new List<EntityProperty>();

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
                    lst.Add(new EntityProperty()
                    {
                        Name = p.Name,
                        Type = p.PropertyType.Name,
                        Size = attrs.OfType<MaxLengthAttribute>().FirstOrDefault()?.Length,
                        Children = new EntityProperty[0]
                    });
                }
                else
                {
                    var complex = new EntityProperty();
                    complex.Name = p.Name;
                    complex.Type = p.PropertyType.Name;
                    complex.Children = GetTypeSchema(p.PropertyType);
                }
            }

            return lst;
        }

        #region IBoundedMappedEntity

        public Guid Id { get; } = Guid.NewGuid();

        public Type SourceType => typeof(TSource);
        public Type TargetType { get; }
        public IReadOnlyCollection<EntityProperty> TargetTypeSchema { get; }
        public string SerializedTargetTypeSchema { get; }
        public Type ContextType => typeof(TSourceContext);

        #endregion
    }

    public interface IBoundedMappedEntity
    {
        Guid Id { get; }
        Type SourceType { get; }
        Type TargetType { get; }
        IReadOnlyCollection<EntityProperty> TargetTypeSchema { get; }
        string SerializedTargetTypeSchema { get; }
        Type ContextType { get; }

        Task<object> GetAndMapByIdAsync(IEntityContext context, int id);
        Task<IReadOnlyCollection<object>> GetAndMapByRangeAsync(IEntityContext context, int fromId, int toId);

        object GetAndMapById(IEntityContext context, int id);
        IReadOnlyCollection<object> GetAndMapByRange(IEntityContext context, int fromId, int toId);
    }
}

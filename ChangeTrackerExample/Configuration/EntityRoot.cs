using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public class EntityRoot<TSourceContext, TSource, TTarget> 
        where TSourceContext : IEntityContext
        where TSource : class, IEntity
        where TTarget : class
    {
        public Expression<Func<TSource, TTarget>> Mapper { get; }

        public IReadOnlyDictionary<string, ParentChildConfiguration> ChildMappings { get; }

        internal EntityRoot(Expression<Func<TSource, TTarget>> mapper)
        {
            Mapper = mapper;
            ChildMappings = new Dictionary<string, ParentChildConfiguration>();
        }

        internal EntityRoot(Expression<Func<TSource, TTarget>> mapper, IReadOnlyDictionary<string, ParentChildConfiguration> dictionary)
        {
            Mapper = mapper;
            ChildMappings = dictionary;
        }

        internal MappedEntityRoot<TSourceContext, TSource, TTarget> Named(string name)
        {
            return new MappedEntityRoot<TSourceContext, TSource, TTarget>(name, Mapper, ChildMappings);
        }

        internal EntityRoot<TSourceContext, TSource, TTarget> WithChild<TChild>(
            Expression<Func<TTarget, TChild>> selector,
            Expression<Func<TChild, int>> foreignKeySelector)
        {
            EnsureHasId(typeof(TChild));
            return WithChildInternal(selector, foreignKeySelector);
        }

        internal EntityRoot<TSourceContext, TSource, TTarget> WithChild<TChild>(
            Expression<Func<TTarget, ICollection<TChild>>> selector,
            Expression<Func<TChild, int>> foreignKeySelector)
        {
            EnsureHasId(typeof(TChild));
            return WithChildInternal(selector, foreignKeySelector);
        }

        internal EntityRoot<TSourceContext, TSource, TTarget> WithChild<TChild>(
            Expression<Func<TTarget, IEnumerable<TChild>>> selector,
            Expression<Func<TChild, int>> foreignKeySelector)
        {
            EnsureHasId(typeof(TChild));
            return WithChildInternal(selector, foreignKeySelector);
        }

        private void EnsureHasId(Type t)
        {
            if (t.GetProperty("Id")?.PropertyType != typeof(int))
            {
                throw new InvalidOperationException($"Type {t.FullName} requires \"id\" property of type {typeof(int).FullName}, because it is used as a complex property");
            }
        }

        private EntityRoot<TSourceContext, TSource, TTarget> WithChildInternal(LambdaExpression selector, LambdaExpression foreignKeySelector)
        {
            var childName = GetPropertyName(selector);
            var foreignKeyPropertyName = GetPropertyName(foreignKeySelector);

            var concat = ChildMappings
                .Concat(new[] {
                    new KeyValuePair<string, ParentChildConfiguration>(childName, new ParentChildConfiguration(foreignKeyPropertyName))
                })
                .ToDictionary(e => e.Key, e => e.Value);

            return new EntityRoot<TSourceContext, TSource, TTarget>(Mapper, concat);
        }

        private static string GetPropertyName(LambdaExpression selector)
        {
            var memberExpression = selector.Body as MemberExpression;

            if (memberExpression == null)
            {
                throw new InvalidOperationException("Invalid labmda expression: expected property selector");
            }

            var property = memberExpression.Member as PropertyInfo;

            if (property == null)
            {
                throw new InvalidOperationException("Invalid labmda expression member: expected property");
            }

            return property.Name;
        }
    }
}

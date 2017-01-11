using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration.Expressions
{
    class ExpressionPathBuilder : ExpressionVisitor
    {
        private readonly Dictionary<object, int> _visited;
        private readonly List<string> _elements;

        private ExpressionPathBuilder()
        {
            _elements = new List<string>();
            _visited = new Dictionary<object, int>();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (!_visited.ContainsKey(node))
            {
                _elements.Add(node.Member.Name);
                _visited[node] = 1;
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (!_visited.ContainsKey(node))
            {
                _elements.Add(node.Type.Name);
                _visited[node] = 1;
            }
            return base.VisitParameter(node);
        }

        public static string GetPropertyOnlyPathExceptRoot(LambdaExpression selector)
        {
            var builder = new ExpressionPathBuilder();
            builder.Visit(selector);
            builder._elements.Reverse();
            return string.Join(".", builder._elements.Skip(1));
        }

        public static string GetPropertyName(LambdaExpression selector)
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

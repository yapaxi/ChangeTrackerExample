using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample.Configuration
{
    public static class ConfigurationExtentions
    {
        public static T Config<T>(this T entity)
        {
            return default(T);
        }

        public static T Config<T>(this T[] entity)
        {

            return default(T);
        }

        public static T Config<T>(this ICollection<T> entity)
        {
            return default(T);

        }

        public static T Config<T>(this IEnumerable<T> entity)
        {
            return default(T);
        }
    }
}

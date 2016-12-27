using Autofac;
using ChangeTrackerExample.Configuration;
using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var queueConnectionString = ConfigurationManager.ConnectionStrings["queue"].ConnectionString;
            var targetDBConnectionString = ConfigurationManager.ConnectionStrings["targetDB"].ConnectionString;
            var sourceDBConnectionString = ConfigurationManager.ConnectionStrings["sourceDB"].ConnectionString;

            var containerBuilder = new ContainerBuilder();

            var changeTrackerBuilder = new ChangeTrackerBuilder(containerBuilder);

            var entitySource = changeTrackerBuilder
                                .RegisterEntity<SomeEntity>()
                                .Map(e => new {
                                    Id = e.Id,
                                    Guid = e.Guid,
                                    Int32 = e.Int32,
                                    Int64 = e.Int64,
                                    YYY = e.MaxString,
                                    XXX = e.ShortString
                                })
                                .FromContext<SourceContext>();

            changeTrackerBuilder.RegisterEntityDestination(entitySource);

        }
    }
}

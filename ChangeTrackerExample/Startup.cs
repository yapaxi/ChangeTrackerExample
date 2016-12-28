using Autofac;
using ChangeTrackerExample.App;
using ChangeTrackerExample.Configuration;
using ChangeTrackerExample.DAL.Contexts;
using ChangeTrackerExample.Domain;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTrackerExample
{
    public partial class Startup
    {
        private static readonly string RABBIT_URI = ConfigurationManager.ConnectionStrings["rabbitUri"].ConnectionString;
        private static readonly string CT_EXCHANGE_1 = ConfigurationManager.ConnectionStrings["ctExchange1"].ConnectionString;
        private static readonly string CT_EXCHANGE_2 = ConfigurationManager.ConnectionStrings["ctExchange2"].ConnectionString;
        private static readonly string CT_LOOPBACK_EXCHANGE = ConfigurationManager.ConnectionStrings["ctLoopbackExchange"].ConnectionString;


        private static readonly string CT_LOOPBACK_QUEUE = ConfigurationManager.ConnectionStrings["ctLoopbackQueue"].ConnectionString;

        private static readonly string DEBUG_CT_EXCHANGE_1_QUEUE = "ha." + CT_EXCHANGE_1 + "-to-console";
        private static readonly string DEBUG_CT_EXCHANGE_2_QUEUE = "ha." + CT_EXCHANGE_2 + "-to-console";

        private readonly IContainer _container;

        public Startup(IContainer container)
        {
            _container = container;
        }
        
    }
}

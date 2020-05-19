using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using TexeraUtilities;
using Engine.OperatorImplementation.Operators;
using Orleans.Runtime.Placement;
using Engine.OperatorImplementation.Common;
using Microsoft.Extensions.DependencyInjection;

namespace SiloHost
{
    public class SiloWrapper
    {
        private static SiloWrapper instance;
        public ISiloHost host;
         

        public static SiloWrapper Instance
        {
            get 
            {
                if (instance == null)
                {
                    instance = new SiloWrapper();
                }
                return instance;
            }
        }

        private SiloWrapper()
        {
            try
            {
                host = StartSilo().Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            var siloBuilder = new SiloHostBuilder()
                .UseAdoNetClustering(options =>
                 {
                     options.ConnectionString = Constants.ConnectionString;
                     options.Invariant = "MySql.Data.MySqlClient";
                 })
                .AddSimpleMessageStreamProvider("SMSProvider")
                // add storage to store list of subscriptions
                .AddMemoryGrainStorage("PubSubStore")
                .UseDashboard(options => {options.Port = 8086; })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "AmberOrleansPrototype";
                })
                .ConfigureServices(services => 
                {
    
                    services.AddSingletonNamedService<PlacementStrategy, WorkerGrainPlacement>(nameof(WorkerGrainPlacement));
                    services.AddSingletonKeyedService<Type, IPlacementDirector, WorkerGrainPlacementDirector>(typeof(WorkerGrainPlacement));
                })
                .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000)
                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Critical).AddConsole())
                .Configure<SiloMessagingOptions>(options => { options.ResponseTimeout = new TimeSpan(1,0,0); });

            var host = siloBuilder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
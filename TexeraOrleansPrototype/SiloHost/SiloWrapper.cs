using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

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
                .UseLocalhostClustering()
                .AddSimpleMessageStreamProvider("SMSProvider")
                // add storage to store list of subscriptions
                .AddMemoryGrainStorage("PubSubStore")
                .UseDashboard(options => {options.Port = 8086; })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "TexeraOrleansPrototype";
                })
                .Configure<EndpointOptions>(options =>
                    options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Critical).AddConsole())
                .Configure<SiloMessagingOptions>(options => { options.ResendOnTimeout = true; options.MaxResendCount = 60; options.ResponseTimeout = new TimeSpan(0,2,0); });

            var host = siloBuilder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
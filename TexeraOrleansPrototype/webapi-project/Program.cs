using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using OrleansClient;
using TexeraUtilities;
namespace webapi
{
    public class Program
    {
        static IClusterClient client;
        public static void Main(string[] args)
        {
            Console.WriteLine("Ready to build connection...");
            client=ClientWrapper.Instance.client;
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseWebRoot("../texera/core/new-gui/dist")
                .UseStartup<Startup>()
                .UseUrls("http://*:7070")
                .Build();
    }
}

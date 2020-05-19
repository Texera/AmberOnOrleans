using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
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

        public static int Main(string[] args)
        => CommandLineApplication.Execute<Program>(args);

        [Option("-c|--clientIP", Description = "IP address of client (MySQL server)")]
        public string ClientIPAddress { get; }=null;

        [Option("-r|--maxretries", Description = "number of max retries when message sending fails")]
        public int MaxRetries { get; }=-1;

        [Option("-b|--batchsize", Description = "number of tuples per batch")]
        public int BatchSize { get; }=-1;

        [Option("-n|--defaultlayersize", Description = "default number of grains per layer per operator")]
        public int DefaultNumGrainsInOneLayer{ get; }=-1;

        private void OnExecute()
        {
            if(DefaultNumGrainsInOneLayer!=-1)
            {
                Constants.DefaultNumGrainsInOneLayer=DefaultNumGrainsInOneLayer;
            }
            if(BatchSize!=-1)
            {
                Constants.BatchSize=BatchSize;
            }
            if(ClientIPAddress!=null)
            {
                Constants.ClientIPAddress=ClientIPAddress;
            }
            if(MaxRetries!=-1)
            {
                Constants.MaxRetries=MaxRetries;
            }
            Console.WriteLine("Ready to build connection...");
            client=ClientWrapper.Instance.client;
            BuildWebHost().Run();
        }

        public static IWebHost BuildWebHost() =>
            WebHost.CreateDefaultBuilder()
                .UseWebRoot("../texera/core/new-gui/dist")
                .UseStartup<Startup>()
                .UseUrls("http://*:7070")
                .Build();
    }
}

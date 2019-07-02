using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using TexeraUtilities;

namespace SiloHost
{
    public class Program
    {
        static ISiloHost host;
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        public static int Main(string[] args)
        => CommandLineApplication.Execute<Program>(args);

        [Option("-c|--clientIP", Description = "IP address of client (MySQL server)")]
        public string ClientIPAddress { get; }="10.138.15.198";

        [Option("-r|--maxretries", Description = "# of max retries when message sending fails")]
        public int MaxRetries { get; }=60;

        [Option("-b|--batchsize", Description = "# of tuples per batch")]
        public int BatchSize { get; }=400;

        [Option("-n|--defaultlayersize", Description = "# of grains per layer per operator")]
        public int DefaultNumGrainsInOneLayer{ get; }=20;

        private void OnExecute()
        {
            Constants.BatchSize=BatchSize;
            Constants.ClientIPAddress=ClientIPAddress;
            Constants.DefaultNumGrainsInOneLayer=DefaultNumGrainsInOneLayer;
            Constants.MaxRetries=MaxRetries;
            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };
            Console.WriteLine("Ready to build connection...");
            host=SiloWrapper.Instance.host;
            Console.WriteLine("Silo Started!");
            _quitEvent.WaitOne();
        }
    }
} 
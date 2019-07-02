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
        public string ClientIPAddress { get; }=null;

        [Option("-r|--maxretries", Description = "# of max retries when message sending fails")]
        public int MaxRetries { get; }=-1;

        [Option("-b|--batchsize", Description = "# of tuples per batch")]
        public int BatchSize { get; }=-1;

        [Option("-n|--defaultlayersize", Description = "# of grains per layer per operator")]
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
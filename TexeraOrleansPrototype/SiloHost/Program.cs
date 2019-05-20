using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public static void Main(string[] args)
        {
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
 namespace SiloHost
{
    public class Program
    {
        static ISiloHost host;
        public static void Main(string[] args)
        {
            host=SiloWrapper.Instance.host;
            Console.WriteLine("Silo Started!");
            Console.ReadLine();
        }
    }
} 
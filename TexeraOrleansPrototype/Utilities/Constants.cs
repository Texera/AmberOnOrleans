using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TexeraUtilities
{
    static public class Constants
    {
        public const string AssemblyPath = "Engine.OperatorImplementation";
        public const int batchSize = 1000;
        public const int num_scan = 10;
        public const bool conditions_on = false;
        public const bool ordered_on = true;
        public const string dataset = "large";
        public const string delivery = "RPC";
        public const string dir= @"/home/sheng/datasets/";
    }
}
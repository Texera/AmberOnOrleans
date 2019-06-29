using Microsoft.Extensions.Logging;
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
        public const string ClientIPAddress="10.138.15.198";
        public const int MaxRetries = 60;
        public const int BatchSize = 400;
        public const int DefaultNumGrainsInOneLayer=10;
        public static string ConnectionString = "server="+ClientIPAddress+";uid=orleans-backend;pwd=orleans-0519-2019;database=orleans;SslMode=none";
    }
}
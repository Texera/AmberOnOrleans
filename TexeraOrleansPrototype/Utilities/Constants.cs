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
        public const string clientIPAddress="10.138.0.5";
        public const int max_retries = 60;
        public const int batchSize = 400;
        public static string connectionString = "server="+clientIPAddress+";uid=orleans-backend;pwd=orleans-0519-2019;database=orleans;SslMode=none";
    }
}
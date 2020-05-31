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
        public static string ClientIPAddress="localhost";
        public static string WebHDFSEntry = "http://localhost:9870/webhdfs/v1/";
        public static int MaxRetries = 60;
        public static int BatchSize = 400;
        public volatile static int DefaultNumGrainsInOneLayer=1;
        public static string ConnectionString 
        {
            get
            {
                return "server="+ClientIPAddress+";uid=orleansbackend;pwd=orleans-0519-2019;database=amberorleans;SslMode=required";
            }
        }
    }
}
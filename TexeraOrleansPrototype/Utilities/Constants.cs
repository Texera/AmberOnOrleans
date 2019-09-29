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
        public static string ClientIPAddress="10.128.0.9";
        public static string WebHDFSEntry = "http://128.195.52.129:9870/webhdfs/v1/";
        public static int MaxRetries = 60;
        public static int BatchSize = 400;
        public static int DefaultNumGrainsInOneLayer=20;
        public static string ConnectionString 
        {
            get
            {
                return "server="+ClientIPAddress+";uid=orleans-backend;pwd=orleans-0519-2019;database=orleans;SslMode=none";
            }
        }
    }
}
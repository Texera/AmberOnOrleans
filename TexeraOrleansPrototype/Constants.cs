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
using TexeraOrleansPrototype.OperatorImplementation.MessagingSemantics;

namespace TexeraOrleansPrototype
{
    static public class Constants
    {
        static public string AssemblyPath = "TexeraOrleansPrototype.OperatorImplementation";
        static public int batchSize = 1000;
    }
}
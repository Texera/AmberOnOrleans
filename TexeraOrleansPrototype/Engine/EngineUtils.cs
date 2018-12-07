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
using Engine.OperatorImplementation.MessagingSemantics;

namespace Engine
{
    static public class Utils
    {
        static public IOrderingEnforcer GetOrderingEnforcerInstance()
        {
            // return new OrderingGrainWithSequenceNumber();
            return new OrderingGrainWithContinuousSending();
        }
        
    }
}
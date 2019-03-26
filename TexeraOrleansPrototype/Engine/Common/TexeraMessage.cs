using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine
{
    public class TexeraMessage
    {
        public ulong sequenceNumber;
        public INormalGrain sender;
        public List<TexeraTuple> tuples;
    }    
}
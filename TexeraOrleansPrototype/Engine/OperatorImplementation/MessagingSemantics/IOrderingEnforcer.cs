using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.MessagingSemantics
{
    public interface IOrderingEnforcer
    {
        List<TexeraTuple> PreProcess(Immutable<TexeraMessage> message);
        ulong GetOutMessageSequenceNumber(INormalGrain nextOperator);
        List<TexeraTuple> CheckStashed(INormalGrain sender);
    }
}
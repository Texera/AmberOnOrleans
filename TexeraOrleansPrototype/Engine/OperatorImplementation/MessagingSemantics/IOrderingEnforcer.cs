using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using TexeraUtilities;

namespace Engine.OperatorImplementation.MessagingSemantics
{
    public interface IOrderingEnforcer
    {
        ulong GetOutgoingSequenceNumber();
        ulong GetExpectedSequenceNumber();
        void IncrementOutgoingSequenceNumber();
        void IncrementExpectedSequenceNumber();
        List<TexeraTuple> PreProcess(List<TexeraTuple> batch, INormalGrain currentOperator);
        Task PostProcess(List<TexeraTuple> batchToForward, INormalGrain currentOperator);
    }
}
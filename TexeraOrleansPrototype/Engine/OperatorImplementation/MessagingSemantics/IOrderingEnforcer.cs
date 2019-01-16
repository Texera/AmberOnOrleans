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
        ulong GetOutgoingSequenceNumber();
        ulong GetExpectedSequenceNumber();
        void IncrementOutgoingSequenceNumber();
        void IncrementExpectedSequenceNumber();
        List<TexeraTuple> PreProcess(List<TexeraTuple> batch, IProcessorGrain currentOperator);
        Task PostProcess(List<TexeraTuple> batchToForward, IProcessorGrain currentOperator, IAsyncStream<Immutable<List<TexeraTuple>>> stream);
    }
}
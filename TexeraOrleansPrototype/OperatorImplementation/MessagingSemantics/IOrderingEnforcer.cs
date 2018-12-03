using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace TexeraOrleansPrototype.OperatorImplementation.MessagingSemantics
{
    public interface IOrderingEnforcer
    {
        ulong GetOutgoingSequenceNumber();
        ulong GetExpectedSequenceNumber();
        void IncrementOutgoingSequenceNumber();
        void IncrementExpectedSequenceNumber();
        List<Tuple> PreProcess(List<Tuple> batch, INormalGrain operatorGrain);
        Task PostProcess(INormalGrain operatorGrain);
    }
}
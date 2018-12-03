using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TexeraOrleansPrototype.OperatorImplementation
{
    public interface INormalGrain : IGrainWithIntegerKey
    {
        Task<INormalGrain> GetNextoperator();
        Task Process(Immutable<List<Tuple>> row);
        Task<Tuple> Process_impl(Tuple tuple);
        Task TrivialCall();
    }
}

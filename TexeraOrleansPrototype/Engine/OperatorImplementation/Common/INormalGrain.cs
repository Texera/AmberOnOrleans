using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Common
{
    public interface INormalGrain : IGrainWithGuidCompoundKey
    {
        Task<INormalGrain> GetNextGrain();
        Task SetIsLastOperatorGrain(bool isLastOperatorGrain);
        Task<bool> GetIsLastOperatorGrain();
        Task SetPredicate(PredicateBase predicate);
        Task SetNextGrain(INormalGrain nextGrain);
        Task PauseGrain();
        Task ResumeGrain();
        Task TrivialCall();
    }
}

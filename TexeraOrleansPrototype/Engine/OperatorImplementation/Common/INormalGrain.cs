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
        Task<IProcessorGrain> GetNextGrain();
        Task SetIsLastOperatorGrain(bool isLastOperatorGrain);
        Task<bool> GetIsLastOperatorGrain();
        Task SetPredicate(PredicateBase predicate);
        Task SetNextGrain(IProcessorGrain nextGrain);
        Task PauseGrain();
        Task ResumeGrain();
        Task Init();
    }
}

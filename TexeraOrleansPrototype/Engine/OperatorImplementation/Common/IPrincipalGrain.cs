using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
namespace Engine.OperatorImplementation.Common
{
    public interface IPrincipalGrain : IGrainWithGuidKey
    {
        Task AddNextPrincipalGrain(IPrincipalGrain nextGrain);
        Task Pause();
        Task Resume();
        Task Init(PredicateBase predicate);
        Task SetPredicate(PredicateBase predicate);
        Task<List<INormalGrain>> GetInputGrains();
        Task Link();
        Task Start();
    }
}
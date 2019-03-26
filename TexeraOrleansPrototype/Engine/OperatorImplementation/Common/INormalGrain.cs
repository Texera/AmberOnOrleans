using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Orleans.Streams;

namespace Engine.OperatorImplementation.Common
{
    public interface INormalGrain : IGrainWithGuidCompoundKey
    {
        Task AddNextStream(IAsyncStream<Immutable<TexeraMessage>> stream);
        Task AddNextGrain(INormalGrain grain);
        Task AddNextGrain(List<INormalGrain> grains);
        Task<bool> NeedCustomSending();
        Task Pause();
        Task Resume();
        Task Init(PredicateBase predicate);
        Task Start();
        Task Receive(Immutable<TexeraMessage> message);
        Task InitSelf();
        Task<List<TexeraTuple>> Process(Immutable<List<TexeraTuple>> message);
        //Task<List<TexeraTuple>> Process_impl(TexeraTuple tuple);

    }
}

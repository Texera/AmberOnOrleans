using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;

namespace Engine.OperatorImplementation
{
    public interface INormalGrain : IGrainWithIntegerKey
    {
        Task<INormalGrain> GetNextoperator();
        Task Process(Immutable<List<TexeraTuple>> row);
        Task<TexeraTuple> Process_impl(TexeraTuple tuple);
        Task PauseGrain();
        Task ResumeGrain();
        Task StartProcessAfterPause();
        Task TrivialCall();
    }
}

using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Common
{
    public interface IProcessorGrain : INormalGrain
    {
        Task ReceiveTuples(Immutable<List<TexeraTuple>> row);
        Task<List<List<TexeraTuple>>> Process(Immutable<List<TexeraTuple>> row);
        Task<TexeraTuple> Process_impl(TexeraTuple tuple);
        // Task StartProcessAfterPause();
    }
}
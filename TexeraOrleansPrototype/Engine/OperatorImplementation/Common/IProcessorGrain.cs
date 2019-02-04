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
        Task ReceiveTuples(Immutable<List<TexeraTuple>> row, IProcessorGrain grain);
        Task<List<List<TexeraTuple>>> Process(Immutable<List<TexeraTuple>> row);
        Task<TexeraTuple> Process_impl(TexeraTuple tuple);
        Task ProcessReceivedTuples(Immutable<List<TexeraTuple>> batch, IProcessorGrain grain);
        Task SendProcessedBatches(Immutable<List<List<TexeraTuple>>> processedBatchList, IProcessorGrain grain);
        Task<Type> GetGrainInterfaceType();
        // Task StartProcessAfterPause();
    }
}
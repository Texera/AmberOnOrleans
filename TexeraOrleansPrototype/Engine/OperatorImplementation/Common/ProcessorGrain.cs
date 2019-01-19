using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.Operators;

namespace Engine.OperatorImplementation.Common
{
    public class ProcessorGrain : NormalGrain, IProcessorGrain
    {
        ConcurrentQueue<Immutable<List<TexeraTuple>>> receivedTuples = new ConcurrentQueue<Immutable<List<TexeraTuple>>>();
        ConcurrentQueue<List<TexeraTuple>> processedTuples = new ConcurrentQueue<List<TexeraTuple>>();

        public override Task OnActivateAsync()
        {
            RegisterTimer((object input) => {return ProcessReceivedTuples();}, null, new TimeSpan(0,0,2), new TimeSpan(0,0,2));
            RegisterTimer((object input) => {return SendProcessedTuples();}, null, new TimeSpan(0,0,2), new TimeSpan(0,0,2));
            return base.OnActivateAsync();
        }

        public async Task ProcessReceivedTuples()
        {
            if(!pause)
            {
                while(receivedTuples.Count != 0)
                {
                    Immutable<List<TexeraTuple>> batch;
                    if(receivedTuples.TryDequeue(out batch))
                    {
                        List<List<TexeraTuple>> processedBatch = await Process(batch);
                        if(processedBatch!=null)
                        {
                            // Console.WriteLine($"Processed batch is count {processedBatch.Count}");
                            foreach(List<TexeraTuple> tupleList in processedBatch)
                            {
                                processedTuples.Enqueue(tupleList);
                            }
                        }
                    }
                }
            }
        }

        public async Task SendProcessedTuples()
        {
            while(processedTuples.Count != 0)
            {
                List<TexeraTuple> batch;
                if(processedTuples.TryDequeue(out batch))
                {
                    if (nextGrain != null)
                    {
                        await (nextGrain).ReceiveTuples(batch.AsImmutable());
                    }
                    else
                    {
                        if(IsLastOperatorGrain)
                        {
                            string extensionKey = "";
                            var streamProvider = GetStreamProvider("SMSProvider");
                            var stream = streamProvider.GetStream<Immutable<List<TexeraTuple>>>(this.GetPrimaryKey(out extensionKey), "Random");
                            await stream.OnNextAsync(batch.AsImmutable());
                        }
                    }
                }
            }
        }

        // public async Task StartProcessAfterPause()
        // {
        //     if(pausedRows.Count > 0)
        //     {
        //         foreach(Immutable<List<TexeraTuple>> batch in pausedRows)
        //         {
        //             Process(batch);
        //         }

        //         // Don't empty the paused row because this is the memory address (kind of) which is
        //         // transferred.
        //         pausedRows = new List<Immutable<List<TexeraTuple>>>();
        //     }

        //     if(nextGrain != null)
        //     {
        //         // assuming that every grain except the start is a ProcessingGrain. So, the next grain
        //         // after a ProcessingGrain will be a ProcessingGrain.
        //         ((ProcessorGrain)nextGrain).StartProcessAfterPause();
        //     }
        // }

        public virtual Task ReceiveTuples(Immutable<List<TexeraTuple>> batch)
        {
            receivedTuples.Enqueue(batch);
            return Task.CompletedTask;
        }

        public virtual async Task<List<List<TexeraTuple>>> Process(Immutable<List<TexeraTuple>> batch)
        {
            List<List<TexeraTuple>> batchList = new List<List<TexeraTuple>>();
            List<TexeraTuple> batchToForward = new List<TexeraTuple>();
            foreach(TexeraTuple tuple in batch.Value)
            {
                TexeraTuple ret = await Process_impl(tuple);
                if(ret != null)
                {
                    batchToForward.Add(ret);
                }                
            }

            batchList.Add(batchToForward);
            
            return batchList;
            // if (batchToForward.Count > 0)
            // {
            //     if (nextGrain != null)
            //         ((ProcessorGrain)nextGrain).Process(batchToForward.AsImmutable());
            // }
        }

        public virtual Task<TexeraTuple> Process_impl(TexeraTuple tuple)
        {
            return null;
        }
    }
}

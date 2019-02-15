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
        // ConcurrentQueue<Immutable<List<TexeraTuple>>> receivedTuples = new ConcurrentQueue<Immutable<List<TexeraTuple>>>();
        // ConcurrentQueue<List<TexeraTuple>> processedTuples = new ConcurrentQueue<List<TexeraTuple>>();

        public override Task OnActivateAsync()
        {
            // RegisterTimer((object input) => {return ProcessReceivedTuples();}, null, new TimeSpan(0,0,2), new TimeSpan(0,0,2));
            // RegisterTimer((object input) => {return SendProcessedTuples();}, null, new TimeSpan(0,0,2), new TimeSpan(0,0,2));
            return base.OnActivateAsync();
        }

        public virtual Task<Type> GetGrainInterfaceType()
        {
            return Task.FromResult(typeof(IProcessorGrain));
        }

        public async Task ProcessReceivedTuples(Immutable<List<TexeraTuple>> batch, IProcessorGrain grain)
        {
            if(!pause)
            {
                List<List<TexeraTuple>> processedBatchList = new List<List<TexeraTuple>>();

                if(pausedRows.Count > 0)
                {
                    foreach (Immutable<List<TexeraTuple>> pausedBatch in pausedRows)
                    {
                        //Console.WriteLine("process paused "+pausedBatch.Value[0].seq_token);
                        List<List<TexeraTuple>> res=await Process(pausedBatch);
                        if(res!=null)processedBatchList.AddRange(res);
                    }
                    pausedRows.Clear();
                }
                if(batch.Value.Count != 0)
                {
                    //Console.WriteLine("process normal "+batch.Value[0].seq_token);
                    List<List<TexeraTuple>> res=await Process(batch);
                    if(res!=null)processedBatchList.AddRange(res);
                }

                await MakeSendProcessedBatchCall(processedBatchList, 0, grain);
            }
            else
            {
                pausedRows.Add(batch);
            }
        }

        public async Task MakeSendProcessedBatchCall(List<List<TexeraTuple>> processedBatchList, int retryCount, IProcessorGrain grain)
        {

            // GrainFactory.GetGrain<IProcessorGrain>(this.GetPrimaryKey(out extensionKey), extensionKey).SendProcessedBatches(processedBatchList.AsImmutable(), 0).ContinueWith((t) =>
            grain.SendProcessedBatches(processedBatchList.AsImmutable(), grain).ContinueWith((t) =>
                {
                    if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                        MakeSendProcessedBatchCall(processedBatchList, retryCount + 1, grain);
                }
            );
        }

        public async Task SendProcessedBatches(Immutable<List<List<TexeraTuple>>> processedBatchList, IProcessorGrain grain)
        {
            if(processedBatchList.Value.Count > 0)
            {
                foreach (List<TexeraTuple> batch in processedBatchList.Value)
                {
                    await SendSingleBatch(batch, 0, grain);
                }
            }
        }

        public async Task SendSingleBatch(List<TexeraTuple> batch, int retryCount, IProcessorGrain grain)
        {
            if(nextGrain != null)
            {
                nextGrain.ReceiveTuples(batch.AsImmutable(), nextGrain).ContinueWith((t)=>
                {
                    if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                        SendSingleBatch(batch, retryCount + 1, grain); 
                });
            }
            else
            {
                if(IsLastOperatorGrain)
                {
                    string extensionKey = "";
                    var streamProvider = GetStreamProvider("SMSProvider");
                    var stream = streamProvider.GetStream<Immutable<List<TexeraTuple>>>(this.GetPrimaryKey(out extensionKey), "Random");
                    stream.OnNextAsync(batch.AsImmutable()).ContinueWith((t)=>
                    {
                        if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                            SendSingleBatch(batch, retryCount + 1, grain);
                    });
                }
            }
        }

        public virtual async Task ReceiveTuples(Immutable<List<TexeraTuple>> batch, IProcessorGrain grain)
        {
            if(batch.Value.Count>0)Console.WriteLine("Receive Tuples called. "+ batch.Value[0].seq_token);
            // receivedTuples.Enqueue(batch);
            await MakeProcessReceivedTuplesCall(batch, 0, grain);
        }

        private async Task MakeProcessReceivedTuplesCall(Immutable<List<TexeraTuple>> batch, int retryCount, IProcessorGrain grain)
        {
            // GrainFactory.GetGrain<IProcessorGrain>(this.GetPrimaryKey(out extensionKey), extensionKey).ProcessReceivedTuples(batch, 0).ContinueWith((t)=>{
            grain.ProcessReceivedTuples(batch, grain).ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    MakeProcessReceivedTuplesCall(batch,retryCount+1,grain);
            });
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
        }

        public virtual Task<TexeraTuple> Process_impl(TexeraTuple tuple)
        {
            return null;
        }
    }
}

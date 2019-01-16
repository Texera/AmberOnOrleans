using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Common
{
    public class ProcessorGrain : NormalGrain, IProcessorGrain
    {

        // public virtual async Task PauseGrain()
        // {
        //     pause = true;

        //     if(nextGrain != null)
        //     {
        //         await nextGrain.PauseGrain();
        //     }   
        // }

        // public virtual async Task ResumeGrain()
        // {
        //     pause = false;
        //     if(nextGrain != null)
        //     {
        //         await nextGrain.ResumeGrain();
        //     }
        // }

        public async Task StartProcessAfterPause()
        {
            if(pausedRows.Count > 0)
            {
                foreach(Immutable<List<TexeraTuple>> batch in pausedRows)
                {
                    Process(batch);
                }

                // Don't empty the paused row because this is the memory address (kind of) which is
                // transferred.
                pausedRows = new List<Immutable<List<TexeraTuple>>>();
            }

            if(nextGrain != null)
            {
                // assuming that every grain except the start is a ProcessingGrain. So, the next grain
                // after a ProcessingGrain will be a ProcessingGrain.
                ((ProcessorGrain)nextGrain).StartProcessAfterPause();
            }
        }

        public virtual async Task Process(Immutable<List<TexeraTuple>> batch)
        {
            List<TexeraTuple> batchToForward = new List<TexeraTuple>();
            foreach(TexeraTuple tuple in batch.Value)
            {
                TexeraTuple ret = await Process_impl(tuple);
                if(ret != null)
                {
                    batchToForward.Add(ret);
                }                
            }
            
            if (batchToForward.Count > 0)
            {
                if (nextGrain != null)
                    ((ProcessorGrain)nextGrain).Process(batchToForward.AsImmutable());
            }
        }

        public virtual Task<TexeraTuple> Process_impl(TexeraTuple tuple)
        {
            return null;
        }
    }
}

using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;

namespace Engine.OperatorImplementation
{
    public class NormalGrain : Grain, INormalGrain
    {
        private ulong current_seq_num = 0;
        public INormalGrain nextOperator = null;

        protected bool pause = false;
        protected List<Immutable<List<TexeraTuple>>> pausedRows = new List<Immutable<List<TexeraTuple>>>();

        public virtual async Task<INormalGrain> GetNextoperator()
        {
            return nextOperator;
        }

        public Task TrivialCall()
        {
            for(int i=0; i< 10000; i++)
            {
                int a = 1;
            }

            return Task.CompletedTask;
        }

        public async Task PauseGrain()
        {
            pause = true;

            if(nextOperator != null)
            {
                await nextOperator.PauseGrain();
            }   
        }

        public async Task ResumeGrain()
        {
            pause = false;
            if(nextOperator != null)
            {
                await nextOperator.ResumeGrain();
            }
        }

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

            if(nextOperator != null)
            {
                nextOperator.StartProcessAfterPause();
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
                if (nextOperator != null)
                    nextOperator.Process(batchToForward.AsImmutable());
            }
        }

        public virtual Task<TexeraTuple> Process_impl(TexeraTuple tuple)
        {
            return null;
        }
    }
}

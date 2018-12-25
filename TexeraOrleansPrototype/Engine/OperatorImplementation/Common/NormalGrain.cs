using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Common
{
    public class NormalGrain : Grain, INormalGrain
    {
        private ulong current_seq_num = 0;
        public INormalGrain nextGrain = null;
        protected PredicateBase predicate = null;
        public bool IsLastOperatorGrain = false;

        protected bool pause = false;
        protected List<Immutable<List<TexeraTuple>>> pausedRows = new List<Immutable<List<TexeraTuple>>>();

        public virtual async Task<INormalGrain> GetNextGrain()
        {
            return nextGrain;
        }

        public Task SetIsLastOperatorGrain(bool isLastOperatorGrain)
        {
            this.IsLastOperatorGrain = isLastOperatorGrain;
            return Task.CompletedTask;
        }

        public async Task<bool> GetIsLastOperatorGrain()
        {
            return IsLastOperatorGrain;
        }

        public Task SetPredicate(PredicateBase predicate)
        {
            this.predicate = predicate;
            return Task.CompletedTask;
        }

        public virtual Task SetNextGrain(INormalGrain nextGrain)
        {
            this.nextGrain = nextGrain;
            return Task.CompletedTask;
        }

        public Task TrivialCall()
        {
            for(int i=0; i< 10000; i++)
            {
                int a = 1;
            }

            return Task.CompletedTask;
        }

        public virtual async Task PauseGrain()
        {
            pause = true;

            if(nextGrain != null)
            {
                await nextGrain.PauseGrain();
            }   
        }

        public virtual async Task ResumeGrain()
        {
            pause = false;
            if(nextGrain != null)
            {
                await nextGrain.ResumeGrain();
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

            if(nextGrain != null)
            {
                nextGrain.StartProcessAfterPause();
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
                    nextGrain.Process(batchToForward.AsImmutable());
            }
        }

        public virtual Task<TexeraTuple> Process_impl(TexeraTuple tuple)
        {
            return null;
        }
    }
}

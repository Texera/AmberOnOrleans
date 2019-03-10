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
        public IProcessorGrain nextGrain = null;
        protected PredicateBase predicate = null;
        public bool IsLastOperatorGrain = false;

        protected bool pause = false;
        protected List<Immutable<List<TexeraTuple>>> pausedRows = new List<Immutable<List<TexeraTuple>>>();

        public virtual async Task<IProcessorGrain> GetNextGrain()
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

        public virtual Task SetNextGrain(IProcessorGrain nextGrain)
        {
            this.nextGrain = nextGrain;
            return Task.CompletedTask;
        }

        public virtual Task Init()
        {
            return Task.CompletedTask;
        }

        public virtual async Task PauseGrain()
        {
            pause = true;

            // if(nextGrain != null)
            // {
            //     await nextGrain.PauseGrain();
            // }   
        }

        public virtual async Task ResumeGrain()
        {
            pause = false;
            // if(nextGrain != null)
            // {
            //     await nextGrain.ResumeGrain();
                // List<TexeraTuple> batch = new List<TexeraTuple>();

                // // make a call with empty batch so that rows received during pause get processed
                // await nextGrain.ReceiveTuples(batch.AsImmutable(), nextGrain);
            // }
        }
    }
}

using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TexeraOrleansPrototype.OperatorImplementation
{
    public class NormalGrain : Grain, INormalGrain
    {
        private ulong current_seq_num = 0;
        public INormalGrain nextOperator = null;

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

        public virtual async Task Process(Immutable<List<Tuple>> batch)
        {
            List<Tuple> batchToForward = new List<Tuple>();
            foreach(Tuple tuple in batch.Value)
            {
                Tuple ret = await Process_impl(tuple);
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

        public virtual Task<Tuple> Process_impl(Tuple tuple)
        {
            return null;
        }
    }
}

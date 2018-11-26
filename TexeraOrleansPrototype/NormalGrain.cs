using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TexeraOrleansPrototype
{
    public class NormalGrain : Grain, INormalGrain
    {
        private ulong current_seq_num = 0;
        public INormalGrain next_op = null;

        public Task TrivialCall()
        {
            for(int i=0; i< 10000; i++)
            {
                int a = 1;
            }

            return Task.CompletedTask;
        }

        public virtual Task Process(List<Immutable<Tuple>> batch)
        {
            Process_impl(ref batch);
            if (batch != null)
            {
                if (next_op is OrderingGrain)
                    batch[0].Value.seq_token = current_seq_num++;
                if (next_op != null)
                    next_op.Process(new List<Immutable<Tuple>>(batch));
            }
            return Task.CompletedTask;
        }

        public virtual Task Process_impl(ref List<Immutable<Tuple>> batch)
        {
            return Task.CompletedTask;
        }
    }
}

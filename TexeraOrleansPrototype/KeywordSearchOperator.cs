// #define PRINT_MESSAGE_ON
//#define PRINT_DROPPED_ON


using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;

namespace TexeraOrleansPrototype
{
    public class OrderedKeywordSearchOperator : OrderingGrain, IKeywordSearchOperator
    {
        bool finished=false;
        private Guid guid = Guid.NewGuid();
        public override Task OnActivateAsync()
        {
            next_op = this.GrainFactory.GetGrain<ICountOperator>(this.GetPrimaryKeyLong());
            return base.OnActivateAsync();
        }

        public Task<Guid> GetStreamGuid()
        {
            return Task.FromResult(guid);
        }

        public override Task Process_impl(ref List<Immutable<Tuple>> batch)
        {
// #if PRINT_MESSAGE_ON
//             Console.WriteLine("Ordered KeywordSearch Process: [" + (row as Tuple).seq_token + "] " + (row as Tuple).id);
// #endif
// #if PRINT_DROPPED_ON
//             if (finished)
//             Console.WriteLine("Ordered KeywordSearch Process: [" + (row as Tuple).seq_token + "] " + (row as Tuple).id);
// #endif

            if(Program.conditions_on)
            {
                foreach(Immutable<Tuple> row in batch)
                {
                    if(!row.Value.region.Contains("Asia"))
                    {
                        batch.Remove(row);
                    }
                }
            }
            
            if (batch[0].Value.id == -1)
            {
                Console.WriteLine("Ordered KeywordSearch done");
                finished = true;
            }
            // else if (cond)
               // (next_op as IOrderedCountOperator).SetAggregatorLevel(true);
            else if(batch.Count == 0)
                batch = null;
            return Task.CompletedTask;
        }
    }
}
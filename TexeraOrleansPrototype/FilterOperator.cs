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
    public class OrderedFilterOperator : OrderingGrain, IFilterOperator
    {
        bool finished = false;
        public override Task OnActivateAsync()
        {
            next_op = base.GrainFactory.GetGrain<IKeywordSearchOperator>(this.GetPrimaryKeyLong());
            return base.OnActivateAsync();
        }

        public override Task Process_impl(ref List<Immutable<Tuple>> batch)
        {
// #if PRINT_MESSAGE_ON
//             Console.WriteLine("Ordered Filter Process: [" + (row as Tuple).seq_token + "] " + (row as Tuple).id);
// #endif
// #if PRINT_DROPPED_ON
//             if (finished)
//             Console.WriteLine("Ordered Filter Process: [" + (row as Tuple).seq_token + "] " + (row as Tuple).id);
// #endif

            if(Program.conditions_on)
            {
                foreach(Immutable<Tuple> row in batch)
                {
                    if(!(row.Value.unit_cost > 50))
                    {
                        batch.Remove(row);
                    }
                }
            }
            // bool cond = Program.conditions_on ? (row as Tuple).unit_cost > 50 : true;
            if (batch[0].Value.id == -1)
            {
                Console.WriteLine("Ordered Filter done");
                finished = true;
            }
            else if(batch.Count == 0)
                batch = null;
            return Task.CompletedTask;
        }
    }

}
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
using TexeraOrleansPrototype.OperatorImplementation.Interfaces;
using TexeraOrleansPrototype.OperatorImplementation.MessagingSemantics;

namespace TexeraOrleansPrototype.OperatorImplementation
{
    public class OrderedFilterOperatorWithSqNum : NormalGrain, IFilterOperator
    {
        bool finished = false;
        IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();

        public override Task OnActivateAsync()
        {
            nextOperator = base.GrainFactory.GetGrain<IKeywordSearchOperator>(this.GetPrimaryKeyLong(), Constants.AssemblyPath);//, "OrderedKeywordSearchOperatorWithSqNum"
            return base.OnActivateAsync();
        }

        public override async Task Process(Immutable<List<Tuple>> batch)
        {
            if(batch.Value.Count == 0)
            {
                Console.WriteLine($"NOT EXPECTED: Filter {this.GetPrimaryKeyLong()} received empty batch.");
                return;
            }

            List<Tuple> batchReceived = orderingEnforcer.PreProcess(batch.Value, this);
            List<Tuple> batchToForward = new List<Tuple>();
            if(batchReceived != null)
            {
                foreach(Tuple tuple in batchReceived)
                {
                    Tuple ret = await Process_impl(tuple);
                    if(ret != null)
                    {
                        batchToForward.Add(ret);
                    }
                }
            }
            await orderingEnforcer.PostProcess(batchToForward, this);
        }

        public override async Task<Tuple> Process_impl(Tuple tuple)
        {
            if(tuple.id != -1 && false)
            {
                return null;
            }

            // bool cond = Program.conditions_on ? (row as Tuple).unit_cost > 50 : true;
            if (tuple.id == -1)
            {
                Console.WriteLine("Ordered Filter done");
                finished = true;
                return tuple;
            }

            return tuple;
        }
    }

}
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
    public class OrderedKeywordSearchOperatorWithSqNum : NormalGrain, IKeywordSearchOperator
    {
        bool finished=false;
        IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();
        private Guid guid = Guid.NewGuid();
        public override Task OnActivateAsync()
        {
            nextOperator = this.GrainFactory.GetGrain<ICountOperator>(this.GetPrimaryKeyLong(), Utils.AssemblyPath);//, "OrderedCountOperatorWithSqNum"
            return base.OnActivateAsync();
        }

        public Task<Guid> GetStreamGuid()
        {
            return Task.FromResult(guid);
        }

        public override async Task Process(Immutable<List<Tuple>> batch)
        {
            List<Tuple> batchReceived = orderingEnforcer.PreProcess(batch.Value, this);
            if(batchReceived != null)
            {
                List<Tuple> batchToForward = new List<Tuple>();
                foreach(Tuple tuple in batchReceived)
                {
                    Tuple ret = await Process_impl(tuple);
                    if(ret != null)
                    {
                        batchToForward.Add(ret);
                    }
                }
                if (batchToForward.Count > 0)
                {
                    if(nextOperator != null)
                    {
                        batchToForward[0].seq_token = orderingEnforcer.GetOutgoingSequenceNumber();
                        orderingEnforcer.IncrementOutgoingSequenceNumber();
                        nextOperator.Process(batchToForward.AsImmutable());
                    }
                    
                }
            }
            await orderingEnforcer.PostProcess(this);
        }

        public override async Task<Tuple> Process_impl(Tuple tuple)
        {
            if(tuple.id != -1 && false)
            {
                return null;
            }
            
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
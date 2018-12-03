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

    public class OrderedCountOperatorWithSqNum : NormalGrain, ICountOperator
    {
        private Guid guid = Guid.NewGuid();
        public bool isIntermediate = false;
        public int count = 0;
        public int intermediateAggregatorsResponded = 0;
        IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();

        public override async Task Process(Immutable<List<Tuple>> batch)
        {
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
                // if (batchToForward.Count > 0)
                // {
                //     if(nextOperator != null)
                //     {
                //         batchToForward[0].seq_token = orderingEnforcer.GetOutgoingSequenceNumber();
                //         orderingEnforcer.IncrementOutgoingSequenceNumber();
                //         nextOperator.Process(batchToForward.AsImmutable());
                //     }
                    
                // }
                
            }
            // await orderingEnforcer.PostProcess(batchToForward, this);
        }

        public override async Task<Tuple> Process_impl(Tuple tuple)
        {
            if (tuple.id == -1)
            {
                ICountFinalOperator finalAggregator = this.GrainFactory.GetGrain<ICountFinalOperator>(1, Constants.AssemblyPath);//, "OrderedCountFinalOperatorWithSqNum"
                // finalAggregator.SetAggregatorLevel(false);
                finalAggregator.SubmitIntermediateAgg(count);
            }
            else
            {
                //Console.WriteLine("Ordered Count processing: [" + (row as Tuple).seq_token + "] " + (row as Tuple).id);
                count++;
            }
            return null;
        }
    }
}
using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using Engine.OperatorImplementation.Interfaces;
using Engine.OperatorImplementation.MessagingSemantics;
using TexeraUtilities;

namespace Engine.OperatorImplementation
{

    public class OrderedCountOperatorWithSqNum : NormalGrain, ICountOperator
    {
        private Guid guid = Guid.NewGuid();
        public bool isIntermediate = false;
        public int count = 0;
        public int intermediateAggregatorsResponded = 0;
        IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();

        public override async Task Process(Immutable<List<TexeraTuple>> batch)
        {
            // Console.Write("Count received batch");
            if(batch.Value.Count == 0)
            {
                Console.WriteLine($"NOT EXPECTED: Count {this.GetPrimaryKeyLong()} received empty batch.");
                return;
            }

            if(pause == true)
            {
                pausedRows.Add(batch);
                return;
            }
            
            List<TexeraTuple> batchReceived = orderingEnforcer.PreProcess(batch.Value, this);
            List<TexeraTuple> batchToForward = new List<TexeraTuple>();
            if(batchReceived != null)
            {
                foreach(TexeraTuple tuple in batchReceived)
                {
                    TexeraTuple ret = await Process_impl(tuple);
                    if(ret != null)
                    {
                        batchToForward.Add(ret);
                    }                
                }
                
            }
            // await orderingEnforcer.PostProcess(batchToForward, this);
        }

        public override async Task<TexeraTuple> Process_impl(TexeraTuple tuple)
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
using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using Engine.OperatorImplementation.MessagingSemantics;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{

    public class CountOperatorGrain : ProcessorGrain, ICountOperatorGrain
    {
        private Guid guid = Guid.NewGuid();
        public bool isIntermediate = false;
        public int count = 0;
        public int intermediateAggregatorsResponded = 0;
        IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();

        public override Task<Type> GetGrainInterfaceType()
        {
            return Task.FromResult(typeof(ICountOperatorGrain));
        }

        public override async Task<List<List<TexeraTuple>>> Process(Immutable<List<TexeraTuple>> batch)
        {
            string extensionKey = "";
            //Console.Write("Count received batch");
            if(batch.Value.Count == 0)
            {
                Console.WriteLine($"NOT EXPECTED: Count {this.GetPrimaryKey(out extensionKey)} received empty batch.");
            }

            // if(pause == true)
            // {
            //     pausedRows.Add(batch);
            //     return;
            // }
            
            List<TexeraTuple> batchReceived = orderingEnforcer.PreProcess(batch.Value, this);
            List<TexeraTuple> batchToForward = new List<TexeraTuple>();
            if(batchReceived != null)
            {
                foreach(TexeraTuple tuple in batchReceived)
                {
                    TexeraTuple ret = await Process_impl(tuple);
                }
            }

            return null;
            // var streamProvider = GetStreamProvider("SMSProvider");
            // var stream = streamProvider.GetStream<Immutable<List<TexeraTuple>>>(this.GetPrimaryKey(), "Random");

            // await orderingEnforcer.PostProcess(batchToForward, this, stream);
        }

        public override async Task<TexeraTuple> Process_impl(TexeraTuple tuple)
        {
            if (tuple.id == -1)
            {
                string extensionKey = "";
                ICountFinalOperatorGrain finalAggregator = this.GrainFactory.GetGrain<ICountFinalOperatorGrain>(this.GetPrimaryKey(out extensionKey), "0", Constants.OperatorAssemblyPathPrefix);//, "CountFinalOperatorGrain"
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
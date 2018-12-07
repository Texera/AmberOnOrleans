using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using Engine.OperatorImplementation.Interfaces;
using TexeraUtilities;

namespace Engine.OperatorImplementation
{

    public class OrderedCountFinalOperatorWithSqNum : NormalGrain, ICountFinalOperator
    {
        private Guid guid = Guid.NewGuid();
        public bool isIntermediate = false;
        public int count = 0;
        public int intermediateAggregatorsResponded = 0;

        public Task<Guid> GetStreamGuid()
        {
            return Task.FromResult(guid);
        }

        public Task SubmitIntermediateAgg(int aggregation)
        {
            count += aggregation;
            intermediateAggregatorsResponded++;

            if (intermediateAggregatorsResponded == Constants.num_scan)
            {
                var streamProvider = GetStreamProvider("SMSProvider");
                var stream = streamProvider.GetStream<int>(guid, "Random");
                stream.OnNextAsync(count);
            }
            return Task.CompletedTask;
        }

        public override async Task Process(Immutable<List<TexeraTuple>> batch)
        {
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
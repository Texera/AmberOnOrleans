using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{

    public class CountFinalOperatorGrain : ProcessorGrain, ICountFinalOperatorGrain
    {
        public bool isIntermediate = false;
        public int count = 0;
        public int intermediateAggregatorsResponded = 0;

        public override Task<Type> GetGrainInterfaceType()
        {
            return Task.FromResult(typeof(ICountFinalOperatorGrain));
        }

        public Task SubmitIntermediateAgg(int aggregation)
        {
            count += aggregation;
            // Console.WriteLine("Count received "+count+" tuples so far");
            intermediateAggregatorsResponded++;

            if (intermediateAggregatorsResponded == Constants.num_scan)
            {
                var streamProvider = GetStreamProvider("SMSProvider");
                string extensionKey = "";
                var stream = streamProvider.GetStream<Immutable<List<TexeraTuple>>>(this.GetPrimaryKey(out extensionKey), "Random");
                // stream.OnNextAsync(count);

                TexeraTuple t = new TexeraTuple((ulong)count, count, null);
                
                if(nextGrain != null)
                {
                    (nextGrain).ReceiveTuples(new List<TexeraTuple>(){t}.AsImmutable(), nextGrain);
                }
                else if(IsLastOperatorGrain)
                {
                    stream.OnNextAsync(new List<TexeraTuple>(){t}.AsImmutable());
                }
            }
            return Task.CompletedTask;
        }

        public override async Task<List<List<TexeraTuple>>> Process(Immutable<List<TexeraTuple>> batch)
        {
            return null;
        }

        public override async Task<TexeraTuple> Process_impl(TexeraTuple tuple)
        {
            return null;
        }
    }

}
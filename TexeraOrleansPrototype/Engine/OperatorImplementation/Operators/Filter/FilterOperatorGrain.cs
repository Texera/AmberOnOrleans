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
using Engine.OperatorImplementation.MessagingSemantics;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{
    public class FilterOperatorGrain : ProcessorGrain, IFilterOperatorGrain
    {
        bool finished = false;
        IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();

        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }

        public override async Task<List<List<TexeraTuple>>> Process(Immutable<List<TexeraTuple>> batch)
        {
            string extensionKey = "";
            // Console.Write(" Filter received batch ");
            if(batch.Value.Count == 0)
            {
                Console.WriteLine($"NOT EXPECTED: Filter {this.GetPrimaryKey(out extensionKey)} received empty batch.");
                return null;
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
                    if(ret != null)
                    {
                        batchToForward.Add(ret);
                    }
                }
            }

            List<List<TexeraTuple>> batchList = new List<List<TexeraTuple>>();

            if(batchToForward.Count > 0)
            {
                orderingEnforcer.PostProcess(ref batchToForward, this);
                batchList.Add(batchToForward);
            }
            List<List<TexeraTuple>> stashedBatches = await orderingEnforcer.ProcessStashed(this);
            batchList.AddRange(stashedBatches);

            return batchList;
            // var streamProvider = GetStreamProvider("SMSProvider");
            // var stream = streamProvider.GetStream<Immutable<List<TexeraTuple>>>(this.GetPrimaryKey(out extensionKey), "Random");
        }

        public override async Task<TexeraTuple> Process_impl(TexeraTuple tuple)
        {
            if(tuple.id != -1 && tuple.unit_cost < ((FilterPredicate)predicate).GetThreshold())
            {
                return null;
            }

            // bool cond = Program.conditions_on ? (row as Tuple).unit_cost > 50 : true;
            if (tuple.id == -1)
            {
                // Console.WriteLine("Ordered Filter done");
                finished = true;
                return tuple;
            }

            return tuple;
        }

        
    }

}
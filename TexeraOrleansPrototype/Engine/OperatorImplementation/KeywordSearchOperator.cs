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
using Engine.OperatorImplementation.Interfaces;
using Engine.OperatorImplementation.MessagingSemantics;
using TexeraUtilities;

namespace Engine.OperatorImplementation
{
    public class OrderedKeywordSearchOperatorWithSqNum : NormalGrain, IKeywordSearchOperator
    {
        bool finished=false;
        IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();
        private Guid guid = Guid.NewGuid();
        public override Task OnActivateAsync()
        {
            nextOperator = this.GrainFactory.GetGrain<ICountOperator>(this.GetPrimaryKeyLong(), Constants.AssemblyPath);
            return base.OnActivateAsync();
        }

        public Task<Guid> GetStreamGuid()
        {
            return Task.FromResult(guid);
        }

        public override async Task Process(Immutable<List<TexeraTuple>> batch)
        {
            Console.Write(" Keyword received batch ");
            if(batch.Value.Count == 0)
            {
                Console.WriteLine($"NOT EXPECTED: Keyword {this.GetPrimaryKeyLong()} received empty batch.");
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
            await orderingEnforcer.PostProcess(batchToForward, this);
        }

        public override async Task<TexeraTuple> Process_impl(TexeraTuple tuple)
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
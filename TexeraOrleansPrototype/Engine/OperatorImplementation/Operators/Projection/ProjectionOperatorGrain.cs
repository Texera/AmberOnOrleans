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
    public class ProjectionOperatorGrain : WorkerGrain, IProjectionOperatorGrain
    {
        List<int> projectionIndexs;

        public override Task OnDeactivateAsync()
        {
            base.OnDeactivateAsync();
            projectionIndexs=null;
            return Task.CompletedTask;
        }

        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            projectionIndexs=((ProjectionPredicate)predicate).ProjectionIndexs;
            return Task.CompletedTask;
        }


        protected override void ProcessTuple(TexeraTuple tuple,List<TexeraTuple> output)
        {
            TexeraTuple result=new TexeraTuple(new string[projectionIndexs.Count]);
            int i=0;
            foreach(int attr in projectionIndexs)
            {
                result.FieldList[i++]=tuple.FieldList[attr];
            }
            output.Add(result);
        }
    }
}
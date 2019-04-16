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
using System.Linq;

namespace Engine.OperatorImplementation.Operators
{
    public class SortOperatorGrain : WorkerGrain, ISortOperatorGrain
    {
        protected override bool WorkAsExternalTask {get{return true;}}
        List<TexeraTuple> sortedTuples=new List<TexeraTuple>();
        int sortIndex;
        int counter=0;
        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            sortIndex=((SortPredicate)predicate).SortIndex;
            return Task.CompletedTask;
        }
        protected override void ProcessTuple(TexeraTuple tuple)
        {
            //Console.WriteLine(++counter+" tuples sorted");
            int idx=-1;
            for(int i=0;i<sortedTuples.Count;++i)
            {
                if(String.Compare(sortedTuples[i].FieldList[sortIndex],tuple.FieldList[sortIndex])==1)
                {
                    idx=i;
                    break;
                }
            }
            if(idx!=-1)
                sortedTuples.Insert(idx,tuple);
            else
                sortedTuples.Add(tuple);
        }

        protected override void MakeFinalOutputTuples()
        {
            outputTuples.AddRange(sortedTuples);
        }
    }

}
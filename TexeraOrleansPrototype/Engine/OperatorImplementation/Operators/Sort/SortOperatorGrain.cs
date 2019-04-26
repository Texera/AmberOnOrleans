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
        List<TexeraTuple> sortedTuples=new List<TexeraTuple>();
        int sortIndex;
        int counter=0;
        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            sortIndex=((SortPredicate)predicate).SortIndex;
            return Task.CompletedTask;
        }
        protected override List<TexeraTuple> ProcessTuple(TexeraTuple tuple)
        {
            //Console.WriteLine(++counter+" tuples sorted");
            bool isNumeric=false;
            float num=0;
            string value=tuple.FieldList[sortIndex];
            try
            {
                num=float.Parse(tuple.FieldList[sortIndex]);
                isNumeric=true;
            }
            catch(Exception)
            {
                
            }
            int idx=-1;
            for(int i=0;i<sortedTuples.Count;++i)
            {
                if(!isNumeric)
                {
                    if(String.Compare(sortedTuples[i].FieldList[sortIndex],value)==1)
                    {
                        idx=i;
                        break;
                    }
                }
                else
                {
                    if(float.Parse(sortedTuples[i].FieldList[sortIndex])>num)
                    {
                        idx=i;
                        break;
                    }
                }
            }
            if(idx!=-1)
                sortedTuples.Insert(idx,tuple);
            else
                sortedTuples.Add(tuple);
            return null;
        }

        protected override List<TexeraTuple> MakeFinalOutputTuples()
        {
            return sortedTuples;
        }
    }

}
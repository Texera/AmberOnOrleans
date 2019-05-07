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
using System.Reflection;

namespace Engine.OperatorImplementation.Operators
{
    public class SortOperatorGrain<T> : WorkerGrain, ISortOperatorGrain<T> where T:IComparable<T>
    {
        private static MethodInfo ParseInfo = typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
        List<TexeraTuple> sortedTuples=new List<TexeraTuple>();
        List<T> sortedValues=new List<T>();
        int sortIndex;
        int counter=0;

        public override Task OnDeactivateAsync()
        {
            base.OnDeactivateAsync();
            sortedValues=null;
            sortedTuples=null;
            return Task.CompletedTask;
        }


        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            sortIndex=((SortPredicate)predicate).SortIndex;
            return Task.CompletedTask;
        }

        private static T Parse(string value)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value;
            else
                return (T)ParseInfo.Invoke(null, new[] { value });
        }

        protected override void ProcessTuple(TexeraTuple tuple,List<TexeraTuple> output)
        {
            T value=Parse(tuple.FieldList[sortIndex]);
            int index = sortedValues.BinarySearch(value);
            if(index<0)
            {
                index=~index;
            }
            sortedTuples.Insert(index,tuple);
            sortedValues.Insert(index,value);
        }

        protected override List<TexeraTuple> MakeFinalOutputTuples()
        {
            return sortedTuples;
        }
    }

}
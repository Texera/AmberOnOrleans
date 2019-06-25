// #define PRINT_MESSAGE_ON
//#define PRINT_DROPPED_ON


using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Orleans.Concurrency;
using Engine.OperatorImplementation.MessagingSemantics;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{

    public class FilterOperatorGrain<T> :WorkerGrain, IFilterOperatorGrain<T> where T:IComparable<T>
    {
        private static MethodInfo ParseInfo = typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
        int filterIndex=-1;
        FilterPredicate.FilterType type;
        T threshold;
        public override async Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            SiloAddress addr=await base.Init(self,predicate,principalGrain);
            if (typeof(T) != typeof(string) && (ParseInfo == null || !typeof(T).IsAssignableFrom(ParseInfo.ReturnType)))
                throw new InvalidOperationException("Invalid type, must contain public static T Parse(string)");
            type=((FilterPredicate)predicate).Type;
            filterIndex=((FilterPredicate)predicate).FilterIndex;
            threshold=Parse(((FilterPredicate)predicate).Threshold);
            return addr;
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
            if(tuple.FieldList!=null)
            {
                switch(type)
                {
                    case FilterPredicate.FilterType.Equal:
                        if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)==0)
                            output.Add(tuple);
                        break;
                    case FilterPredicate.FilterType.Greater:
                        if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)>0)
                            output.Add(tuple);
                        break;
                    case FilterPredicate.FilterType.GreaterOrEqual:
                        if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)>=0)
                            output.Add(tuple);
                        break;
                    case FilterPredicate.FilterType.Less:
                        if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)<0)
                            output.Add(tuple);
                        break;
                    case FilterPredicate.FilterType.LessOrEqual:
                        if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)<=0)
                            output.Add(tuple);
                        break;
                    case FilterPredicate.FilterType.NotEqual:
                        if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)!=0)
                            output.Add(tuple);
                        break;
                }   
            }
        }
    }
}
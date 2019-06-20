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
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{
    public class GroupByOperatorGrain : WorkerGrain, IGroupByOperatorGrain
    {
        Dictionary<string,double> results=new Dictionary<string, double>();
        Dictionary<string,int> counter=new Dictionary<string, int>();
        int groupByIndex;
        int aggregationIndex;
        GroupByPredicate.AggregationType aggregationFunc;

        public override Task OnDeactivateAsync()
        {
            base.OnDeactivateAsync();
            results=null;
            counter=null;
            return Task.CompletedTask;
        }

        public override async Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            SiloAddress addr=await base.Init(self,predicate,principalGrain);
            groupByIndex=((GroupByPredicate)predicate).GroupByIndex;
            aggregationFunc=((GroupByPredicate)predicate).Aggregation;
            aggregationIndex=((GroupByPredicate)predicate).AggregationIndex;
            return addr;
        }


        protected override void ProcessTuple(in TexeraTuple tuple,List<TexeraTuple> output)
        {
            string field=tuple.FieldList[groupByIndex];
            if(counter.ContainsKey(field))
            {
                counter[field]++;
            }
            else
            {
                counter[field]=1;
            }
            if(aggregationFunc!=GroupByPredicate.AggregationType.Count)
            {
                try
                {
                    double value=double.Parse(tuple.FieldList[aggregationIndex]);
                    if(results.ContainsKey(field))
                    {
                        double oldValue=results[field];
                        switch(aggregationFunc)
                        {
                            case GroupByPredicate.AggregationType.Max:
                                results[field]=Math.Max(oldValue,value);
                                break;
                            case GroupByPredicate.AggregationType.Min:
                                results[field]=Math.Min(oldValue,value);
                                break;
                            case GroupByPredicate.AggregationType.Average:
                                results[field]+=value;
                                break;
                            case GroupByPredicate.AggregationType.Sum:
                                results[field]+=value;
                                break;
                        }
                    }
                    else
                    {
                        results[field]=value;
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        protected override List<TexeraTuple> MakeFinalOutputTuples()
        {
            List<TexeraTuple> result=new List<TexeraTuple>();
            foreach(KeyValuePair<string,int> pair in counter)
            {
                Console.WriteLine(pair.Key);
                switch(aggregationFunc)
                {
                    case GroupByPredicate.AggregationType.Max:
                        result.Add(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString()}));
                        break;
                    case GroupByPredicate.AggregationType.Min:
                        result.Add(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString()}));
                        break;
                    case GroupByPredicate.AggregationType.Average:
                        result.Add(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString(),pair.Value.ToString()}));
                        break;
                    case GroupByPredicate.AggregationType.Sum:
                        result.Add(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString()}));
                        break;
                    case GroupByPredicate.AggregationType.Count:
                        result.Add(new TexeraTuple(new string[]{pair.Key,pair.Value.ToString()}));
                        break;
                }
            }
            return result;
        }
    }
}
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
    public class GroupByOperatorGrain : WorkerGrain, IGroupByOperatorGrain
    {
        Dictionary<string,double> results=new Dictionary<string, double>();
        Dictionary<string,int> counter=new Dictionary<string, int>();
        int groupByIndex;
        int aggregationIndex;
        string aggregationFunc;

        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            groupByIndex=((GroupByPredicate)predicate).GroupByIndex;
            aggregationFunc=((GroupByPredicate)predicate).AggregationFunction;
            aggregationIndex=((GroupByPredicate)predicate).AggregationIndex;
            return Task.CompletedTask;
        }


        protected override List<TexeraTuple> ProcessTuple(TexeraTuple tuple)
        {
            try
            {
                string field=tuple.FieldList[groupByIndex];
                double value=double.Parse(tuple.FieldList[aggregationIndex]);
                if(results.ContainsKey(field))
                {
                    counter[field]++;
                    double oldValue=results[field];
                    switch(aggregationFunc)
                    {
                        case "max":
                            results[field]=Math.Max(oldValue,value);
                            break;
                        case "min":
                            results[field]=Math.Min(oldValue,value);
                            break;
                        case "avg":
                            results[field]+=value;
                            break;
                        case "sum":
                            results[field]+=value;
                            break;
                        case "count":
                            break;
                    }
                }
                else
                {
                    results[field]=value;
                    counter[field]=1;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }
        protected override List<TexeraTuple> MakeFinalOutputTuples()
        {
            List<TexeraTuple> result=new List<TexeraTuple>();
            foreach(KeyValuePair<string,double> pair in results)
            {
                switch(aggregationFunc)
                {
                    case "max":
                        result.Add(new TexeraTuple(-1,new string[]{pair.Key,pair.Value.ToString()}));
                        break;
                    case "min":
                        result.Add(new TexeraTuple(-1,new string[]{pair.Key,pair.Value.ToString()}));
                        break;
                    case "avg":
                        result.Add(new TexeraTuple(-1,new string[]{pair.Key,(pair.Value/counter[pair.Key]).ToString()}));
                        break;
                    case "sum":
                        result.Add(new TexeraTuple(-1,new string[]{pair.Key,pair.Value.ToString()}));
                        break;
                    case "count":
                        result.Add(new TexeraTuple(-1,new string[]{pair.Key,counter[pair.Key].ToString()}));
                        break;
                }
            }
            return result;
        }
    }
}
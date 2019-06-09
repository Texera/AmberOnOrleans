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
using Orleans.Placement;
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{
    public class GroupByFinalOperatorGrain : WorkerGrain, IGroupByFinalOperatorGrain
    {
        Dictionary<string,double> results=new Dictionary<string, double>();
        Dictionary<string,int> counter=new Dictionary<string, int>();
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
            aggregationFunc=((GroupByPredicate)predicate).Aggregation;
            return addr;
        }

        protected override void ProcessTuple(in TexeraTuple tuple, List<TexeraTuple> output)
        {
            string field=tuple.FieldList[0];
            if(aggregationFunc==GroupByPredicate.AggregationType.Count)
            {
                if(counter.ContainsKey(field))
                    counter[field]+=int.Parse(tuple.FieldList[1]);
                else
                    counter.Add(field,int.Parse(tuple.FieldList[1]));
            }
            else
            {
                double value=double.Parse(tuple.FieldList[1]);
                if(results.ContainsKey(field))
                {
                    switch(aggregationFunc)
                    {
                        case GroupByPredicate.AggregationType.Max:
                            results[field]=Math.Max(results[field],value);
                            break;
                        case GroupByPredicate.AggregationType.Min:
                            results[field]=Math.Min(results[field],value);
                            break;
                        case GroupByPredicate.AggregationType.Average:
                            results[field]+=value;
                            counter[field]+=int.Parse(tuple.FieldList[2]);
                            break;
                        case GroupByPredicate.AggregationType.Sum:
                            results[field]+=value;
                            break;
                    }
                }
                else
                {
                    results.Add(field,value);
                    counter.Add(field,1);
                }
            }
        }

        protected override List<TexeraTuple> MakeFinalOutputTuples()
        {
            List<TexeraTuple> result=new List<TexeraTuple>();
            foreach(KeyValuePair<string,int> pair in counter)
            {
                switch(aggregationFunc)
                {
                    case GroupByPredicate.AggregationType.Max:
                        result.Add(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString()}));
                        break;
                    case GroupByPredicate.AggregationType.Min:
                        result.Add(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString()}));
                        break;
                    case GroupByPredicate.AggregationType.Average:
                        result.Add(new TexeraTuple(new string[]{pair.Key,(results[pair.Key]/pair.Value).ToString()}));
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
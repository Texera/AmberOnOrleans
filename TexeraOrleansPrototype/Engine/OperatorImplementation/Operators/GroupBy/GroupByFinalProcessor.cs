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
    public class GroupByFinalProcessor : ITupleProcessor
    {
        Dictionary<string,double> results;
        Dictionary<string,int> counter;
        AggregationType aggregationFunc;
        Queue<TexeraTuple> finalResult;

        public GroupByFinalProcessor(AggregationType aggregation)
        {
            aggregationFunc = aggregation;
        }

        public void Accept(TexeraTuple tuple)
        {
            string field=tuple.FieldList[0];
            if(aggregationFunc==AggregationType.Count)
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
                        case AggregationType.Max:
                            results[field]=Math.Max(results[field],value);
                            break;
                        case AggregationType.Min:
                            results[field]=Math.Min(results[field],value);
                            break;
                        case AggregationType.Average:
                            results[field]+=value;
                            counter[field]+=int.Parse(tuple.FieldList[2]);
                            break;
                        case AggregationType.Sum:
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

        public void OnRegisterSource(Guid from)
        {
            
        }

        public void NoMore()
        {
            foreach(KeyValuePair<string,int> pair in counter)
            {
                switch(aggregationFunc)
                {
                    case AggregationType.Max:
                        finalResult.Enqueue(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString()}));
                        break;
                    case AggregationType.Min:
                        finalResult.Enqueue(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString()}));
                        break;
                    case AggregationType.Average:
                        finalResult.Enqueue(new TexeraTuple(new string[]{pair.Key,(results[pair.Key]/pair.Value).ToString()}));
                        break;
                    case AggregationType.Sum:
                        finalResult.Enqueue(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString()}));
                        break;
                    case AggregationType.Count:
                        finalResult.Enqueue(new TexeraTuple(new string[]{pair.Key,pair.Value.ToString()}));
                        break;
                }
            }
        }

        public Task Initialize()
        {
            results = new Dictionary<string, double>();
            counter = new Dictionary<string, int>();
            finalResult = new Queue<TexeraTuple>();
            return Task.CompletedTask;
        }

        public bool HasNext()
        {
            return finalResult.Count > 0;
        }

        public TexeraTuple Next()
        {
            return finalResult.Dequeue();
        }

        public void Dispose()
        {
            results=null;
            counter=null;
        }

        public Task<TexeraTuple> NextAsync()
        {
            throw new NotImplementedException();
        }

        public void MarkSourceCompleted(Guid source)
        {
            
        }
    }

}
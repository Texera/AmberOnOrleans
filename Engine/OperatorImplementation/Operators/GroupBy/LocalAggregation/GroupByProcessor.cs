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
    public class GroupByProcessor :ITupleProcessor
    {
        Dictionary<string,double> results;
        Dictionary<string,int> counter;
        Queue<TexeraTuple> finalResult;
        int groupByIndex;
        int aggregationIndex;
        AggregationType aggregationFunc;

        
        public GroupByProcessor(int groupByIndex,AggregationType aggregation,int aggregationIndex)
        {
            this.groupByIndex=groupByIndex;
            this.aggregationFunc=aggregation;
            this.aggregationIndex=aggregationIndex;
        }

        public void Accept(TexeraTuple tuple)
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
            if(aggregationFunc!=AggregationType.Count)
            {
                try
                {
                    double value=double.Parse(tuple.FieldList[aggregationIndex]);
                    if(results.ContainsKey(field))
                    {
                        double oldValue=results[field];
                        switch(aggregationFunc)
                        {
                            case AggregationType.Max:
                                results[field]=Math.Max(oldValue,value);
                                break;
                            case AggregationType.Min:
                                results[field]=Math.Min(oldValue,value);
                                break;
                            case AggregationType.Average:
                                results[field]+=value;
                                break;
                            case AggregationType.Sum:
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
                        finalResult.Enqueue(new TexeraTuple(new string[]{pair.Key,results[pair.Key].ToString(),pair.Value.ToString()}));
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
            results=new Dictionary<string, double>();
            counter=new Dictionary<string, int>();
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
            results = null;
            counter = null;
            finalResult = null;
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
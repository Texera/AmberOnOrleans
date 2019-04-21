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
    public class FilterOperatorGrain : WorkerGrain, IFilterOperatorGrain
    {

        int filterIndex=-1;
        FilterPredicate.FilterType type;
        float threshold=0;
        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            type=((FilterPredicate)predicate).Type;
            filterIndex=((FilterPredicate)predicate).FilterIndex;
            threshold=((FilterPredicate)predicate).Threshold;
            return Task.CompletedTask;
        }


        protected override void ProcessBatch(List<TexeraTuple> tuples, ref List<TexeraTuple> output)
        {
            foreach(TexeraTuple tuple in tuples)
            {
                if(tuple.FieldList!=null)
                {
                    switch(type)
                    {
                        case FilterPredicate.FilterType.Equal:
                            if(float.Parse(tuple.FieldList[filterIndex])==threshold)
                                output.Add(tuple);
                            break;
                        case FilterPredicate.FilterType.Greater:
                            if(float.Parse(tuple.FieldList[filterIndex])>threshold)
                                output.Add(tuple);
                            break;
                        case FilterPredicate.FilterType.GreaterOrEqual:
                            if(float.Parse(tuple.FieldList[filterIndex])>=threshold)
                                output.Add(tuple);
                            break;
                        case FilterPredicate.FilterType.Less:
                            if(float.Parse(tuple.FieldList[filterIndex])<threshold)
                                output.Add(tuple);
                            break;
                        case FilterPredicate.FilterType.LessOrEqual:
                            if(float.Parse(tuple.FieldList[filterIndex])<=threshold)
                                output.Add(tuple);
                            break;
                        case FilterPredicate.FilterType.NotEqual:
                            if(float.Parse(tuple.FieldList[filterIndex])!=threshold)
                                output.Add(tuple);
                            break;
                    }   
                }
            }
        }
    }
}
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
    public class KeywordSearchOperatorGrain : WorkerGrain, IKeywordSearchOperatorGrain
    {
        int searchIndex;
        string keyword;

        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            searchIndex=((KeywordPredicate)predicate).SearchIndex;
            keyword=((KeywordPredicate)predicate).Query;
            return Task.CompletedTask;
        }


        protected override void ProcessTuple(TexeraTuple tuple)
        {
            if(tuple.FieldList!=null && tuple.FieldList[searchIndex].Contains(keyword))
                outputTuples.Add(tuple);
        }
    }
}
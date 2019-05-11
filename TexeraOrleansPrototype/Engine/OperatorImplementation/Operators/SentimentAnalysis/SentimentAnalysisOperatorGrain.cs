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
using SentimentAnalyzer;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{
    public class SentimentAnalysisOperatorGrain : WorkerGrain, ISentimentAnalysisOperatorGrain
    {
        int predictIndex;
        public override Task OnDeactivateAsync()
        {
            base.OnDeactivateAsync();
            return Task.CompletedTask;
        }

        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            predictIndex=((SentimentAnalysisPredicate)predicate).PredictIndex;
            return Task.CompletedTask;
        }


        protected override void ProcessTuple(TexeraTuple tuple,List<TexeraTuple> output)
        {
            var result=SentimentAnalyzer.Sentiments.Predict(tuple.FieldList[predictIndex]);
            output.Add(new TexeraTuple(new string[]{tuple.FieldList[predictIndex],$"{(result.Prediction?"positive":"negative")}(Prob:{result.Probability},Score:{result.Score})"}));
        }
    }
}
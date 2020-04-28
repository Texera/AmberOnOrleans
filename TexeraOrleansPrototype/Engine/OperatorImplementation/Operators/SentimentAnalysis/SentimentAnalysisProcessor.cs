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
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{
    public class SentimentAnalysisProcessor : ITupleProcessor
    {
        int predictIndex;
        TexeraTuple resultTuple;
        bool flag = false;

        public SentimentAnalysisProcessor(int predictIndex)
        {
            this.predictIndex=predictIndex;
        }

        public void Accept(TexeraTuple tuple)
        {
            flag=true;
            var result=SentimentAnalyzer.Sentiments.Predict(tuple.FieldList[predictIndex]);
            resultTuple = new TexeraTuple(new string[]{tuple.FieldList[predictIndex],$"{(result.Prediction?"positive":"negative")}(Prob:{result.Probability},Score:{result.Score})"});
        }

        public void Dispose()
        {
            
        }

        public bool HasNext()
        {
            return flag;
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public void MarkSourceCompleted(Guid source)
        {
            
        }

        public TexeraTuple Next()
        {
            flag=false;
            return resultTuple;
        }

        public Task<TexeraTuple> NextAsync()
        {
            throw new NotImplementedException();
        }

        public void NoMore()
        {
            
        }

        public void OnRegisterSource(Guid from)
        {
            
        }
    }
}
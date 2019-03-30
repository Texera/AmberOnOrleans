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
        protected override void ProcessBatch(List<TexeraTuple> tuples, ref List<TexeraTuple> output)
        {
            foreach(TexeraTuple tuple in tuples)
            {
                if(tuple.FieldList!=null && tuple.FieldList[0].Contains("Asia"))
                    output.Add(tuple);
            }
        }
    }
}
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
    public class CountOperatorGrain : WorkerGrain, ICountOperatorGrain
    {
        protected override bool WorkAsExternalTask {get{return true;}}
        int count=0;
        protected override void ProcessTuple(TexeraTuple tuple)
        {
            count++;
        }

        protected override void MakeFinalOutputTuples()
        {
            outputTuples.Add(new TexeraTuple(-1,new string[]{count.ToString()}));
        }
    }
}
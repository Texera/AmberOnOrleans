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
        int count=0;
        protected override List<TexeraTuple> ProcessTuple(TexeraTuple tuple)
        {
            count++;
            return null;
        }

        protected override List<TexeraTuple> MakeFinalOutputTuples()
        {
            return new List<TexeraTuple>{new TexeraTuple(-1,new string[]{count.ToString()})};
        }
    }
}
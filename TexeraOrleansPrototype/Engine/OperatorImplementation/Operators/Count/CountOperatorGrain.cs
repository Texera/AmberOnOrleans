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
using Orleans.Placement;

namespace Engine.OperatorImplementation.Operators
{
    [PreferLocalPlacement]
    public class CountOperatorGrain : WorkerGrain, ICountOperatorGrain
    {
        int count=0;
        protected override void ProcessTuple(TexeraTuple tuple,List<TexeraTuple> output)
        {
            count++;
        }

        protected override List<TexeraTuple> MakeFinalOutputTuples()
        {
            return new List<TexeraTuple>{new TexeraTuple(new string[]{count.ToString()})};
        }
    }
}
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

namespace Engine.OperatorImplementation.Operators
{

    public class CountFinalOperatorGrain : WorkerGrain, ICountFinalOperatorGrain
    {
        public int count = 0;
        protected override List<TexeraTuple> ProcessTuple(TexeraTuple tuple)
        {
            count+=int.Parse(tuple.FieldList[0]);
            Console.WriteLine("current count: "+count);
            return null;
        }

        protected override List<TexeraTuple> MakeFinalOutputTuples()
        {
            return new List<TexeraTuple>{new TexeraTuple(-1,new string[]{count.ToString()})};
        }
    }

}
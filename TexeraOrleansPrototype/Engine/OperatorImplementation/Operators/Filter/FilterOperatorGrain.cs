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
        protected override List<TexeraTuple> ProcessTuple(TexeraTuple tuple)
        {
            if(tuple.FieldList!=null && int.Parse(tuple.FieldList[9])>50)
            {
                return new List<TexeraTuple>{tuple};
            }
            return null;
        }
    }

}
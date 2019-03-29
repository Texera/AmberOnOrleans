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
        protected override int BatchingLimit {get{return 1;}}
        protected override List<TexeraTuple> ProcessTuple(TexeraTuple tuple)
        {
            count+=tuple.CustomResult;
            return null;
        }

        protected override void MakeFinalPayloadMessage(ref List<PayloadMessage> outputMessages)
        {
            List<TexeraTuple> payload=new List<TexeraTuple>{new TexeraTuple(-1,null,count)};
            outputMessages.Add(new PayloadMessage(MakeIdentifier(this),0,payload,false));
        }
    }

}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public abstract class SingleQueueBatching
    {
        private Queue<TexeraTuple> outputRows=new Queue<TexeraTuple>();
        private int SingleQueueBatchingLimit;
        public SingleQueueBatching(int SingleQueueBatchingLimit=1000)
        {
            this.SingleQueueBatchingLimit=SingleQueueBatchingLimit;
        }

        public void Enqueue(List<TexeraTuple> output)
        {
            foreach(TexeraTuple tuple in output)
            {
                outputRows.Enqueue(tuple);
            }
        }

        protected PayloadMessage MakeBatchedMessage(string senderIdentifier,ulong sequenceNumber)
        {
            PayloadMessage outputMessage=null;
            if(outputRows.Count>=SingleQueueBatchingLimit)
            {
                List<TexeraTuple> payload=new List<TexeraTuple>();
                for(int i=0;i<SingleQueueBatchingLimit;++i)
                {
                    payload.Add(outputRows.Dequeue());
                }
                outputMessage=new PayloadMessage(senderIdentifier,sequenceNumber,payload,false);
            }
            return outputMessage;
        }

        protected PayloadMessage MakeLastMessage(string senderIdentifier,ulong sequenceNumber)
        {
            if(outputRows.Count==0)return null;
            List<TexeraTuple> payload=new List<TexeraTuple>();
            payload.AddRange(outputRows);
            return new PayloadMessage(senderIdentifier,sequenceNumber,payload,false);
        }
    }
}
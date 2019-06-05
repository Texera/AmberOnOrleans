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
        private int batchingLimit;
        public SingleQueueBatching(int batchingLimit=1000)
        {
            this.batchingLimit=batchingLimit;
        }

        public void Enqueue(IEnumerable<TexeraTuple> output)
        {
            foreach(TexeraTuple tuple in output)
            {
                if(tuple ==null)
                {
                    Console.WriteLine("???? from enqueue");
                }
                outputRows.Enqueue(tuple);
            }
        }

        protected PayloadMessage MakeBatchedMessage(IGrain senderIdentifier,ulong sequenceNumber)
        {
            PayloadMessage outputMessage=null;
            lock(outputRows)
            {
                if(outputRows.Count>=batchingLimit)
                {
                    List<TexeraTuple> payload=new List<TexeraTuple>();
                    for(int i=0;i<batchingLimit;++i)
                    {
                        TexeraTuple tuple=outputRows.Dequeue();
                        if(tuple==null)
                        {
                            Console.WriteLine("???? from outputRows.Dequeue");
                        }
                        payload.Add(tuple);
                    }
                    if(payload[0]==null)
                    {
                        Console.WriteLine("???? from MakeBatchedMessage");
                    }
                    outputMessage=new PayloadMessage(senderIdentifier,sequenceNumber,payload,false);
                }
            }
            return outputMessage;
        }

        protected PayloadMessage MakeLastMessage(IGrain senderIdentifier,ulong sequenceNumber)
        {
            if(outputRows.Count==0)return null;
            List<TexeraTuple> payload=new List<TexeraTuple>();
            payload.AddRange(outputRows);
            outputRows=new Queue<TexeraTuple>();
            return new PayloadMessage(senderIdentifier,sequenceNumber,payload,false);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public class Shuffle: MultiQueueSendStrategy
    {
        private Func<TexeraTuple,int> selector;
        public Shuffle(List<IWorkerGrain> receivers,Func<TexeraTuple,int> selector, int batchingLimit=1000):base(receivers,batchingLimit)
        {
            this.selector=selector;
        }

        public override void Enqueue(List<TexeraTuple> output)
        {
            foreach(TexeraTuple tuple in output)
            {
                outputRows[selector(tuple)%outputRows.Count].Enqueue(tuple);
            }
        }

        public override void AddReceiver(IWorkerGrain receiver)
        {
            receivers.Add(receiver);
            this.outputSequenceNumbers.Add(0);
            this.outputRows.Add(new Queue<TexeraTuple>());
        }

        public override void AddReceivers(List<IWorkerGrain> receivers)
        {
            this.receivers.AddRange(receivers);
            this.outputSequenceNumbers.AddRange(Enumerable.Repeat((ulong)0,receivers.Count));
            this.outputRows.AddRange(Enumerable.Range(0,receivers.Count).Select(x=>new Queue<TexeraTuple>()));
        }


        public override void SendBatchedMessages(string senderIdentifier)
        {
            foreach(Pair<int,List<TexeraTuple>> pair in MakeBatchedPayloads())
            {
                SendMessageTo(receivers[pair.First],new PayloadMessage(senderIdentifier,outputSequenceNumbers[pair.First]++,pair.Second,false).AsImmutable(),0);
            }
        }

        public override void SendEndMessages(string senderIdentifier)
        {
            foreach(Pair<int,List<TexeraTuple>> pair in MakeLastPayload())
            {
                SendMessageTo(receivers[pair.First],new PayloadMessage(senderIdentifier,outputSequenceNumbers[pair.First]++,pair.Second,false).AsImmutable(),0);
            }
            for(int i=0;i<receivers.Count;++i)
            {
                PayloadMessage message = new PayloadMessage(senderIdentifier,outputSequenceNumbers[i]++,null,true);
                SendMessageTo(receivers[i],message.AsImmutable(),0);
            }

        }

    }
}
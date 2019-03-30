using System;
using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine
{
    public class RoundRobin: SingleQueueSendStrategy
    {
        private int roundRobinIndex=0;
        public RoundRobin(List<IWorkerGrain> receivers, int batchingLimit=1000):base(receivers,batchingLimit)
        {
        }

        public override void SendBatchedMessages(string senderIdentifier)
        {
            while(true)
            {
                PayloadMessage message = MakeBatchedMessage(senderIdentifier,outputSequenceNumbers[roundRobinIndex]);
                if(message==null)
                {
                    break;
                }
                RoundRobinSending(message.AsImmutable());
            }
        }

        public override void SendEndMessages(string senderIdentifier)
        {
            PayloadMessage message=MakeLastMessage(senderIdentifier,outputSequenceNumbers[roundRobinIndex]);
            if(message!=null)
            {
                RoundRobinSending(message.AsImmutable());
            }
            for(int i=0;i<receivers.Count;++i)
            {
                message = new PayloadMessage(senderIdentifier,0,null,true);
                message.SequenceNumber=outputSequenceNumbers[i]++;
                SendMessageTo(receivers[i],message.AsImmutable(),0);
            }
        }

        private void RoundRobinSending(Immutable<PayloadMessage> message)
        {
            outputSequenceNumbers[roundRobinIndex]++;
            SendMessageTo(receivers[roundRobinIndex],message,0);
            roundRobinIndex++;
            roundRobinIndex%=receivers.Count;
        }
    }
}
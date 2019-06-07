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
    public class RoundRobin: SingleQueueSendStrategy
    {
        private int roundRobinIndex=0;
        public RoundRobin(int batchingLimit=1000):base(batchingLimit)
        {
        }

        public override void SendBatchedMessages(IGrain senderIdentifier)
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

        public override void SendEndMessages(IGrain senderIdentifier)
        {
            PayloadMessage message=MakeLastMessage(senderIdentifier,outputSequenceNumbers[roundRobinIndex]);
            if(message!=null)
            {
                RoundRobinSending(message.AsImmutable());
            }
            for(int i=0;i<receivers.Count;++i)
            {
                message = new PayloadMessage(senderIdentifier,outputSequenceNumbers[i]++,null,true);
                receivers[i].Send(message.AsImmutable());
            }
        }

        private void RoundRobinSending(Immutable<PayloadMessage> message)
        {
            outputSequenceNumbers[roundRobinIndex]++;
            receivers[roundRobinIndex].Send(message);
            roundRobinIndex = (roundRobinIndex+1)%receivers.Count;
        }
    }
}
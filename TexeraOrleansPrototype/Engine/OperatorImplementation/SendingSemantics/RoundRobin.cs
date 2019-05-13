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
        public RoundRobin(List<IWorkerGrain> receivers, int batchingLimit=1000):base(receivers,batchingLimit)
        {
        }

        public override async void SendBatchedMessages(IGrain senderIdentifier)
        {
            while(true)
            {
                PayloadMessage message = MakeBatchedMessage(senderIdentifier,outputSequenceNumbers[roundRobinIndex]);
                if(message==null)
                {
                    break;
                }
                await RoundRobinSending(message.AsImmutable());
            }
        }

        public override async void SendEndMessages(IGrain senderIdentifier)
        {
            PayloadMessage message=MakeLastMessage(senderIdentifier,outputSequenceNumbers[roundRobinIndex]);
            if(message!=null)
            {
                await RoundRobinSending(message.AsImmutable());
            }
            for(int i=0;i<receivers.Count;++i)
            {
                message = new PayloadMessage(senderIdentifier,outputSequenceNumbers[i]++,null,true);
                await SendMessageTo(receivers[i],message.AsImmutable(),0);
            }
        }

        private async Task RoundRobinSending(Immutable<PayloadMessage> message)
        {
            outputSequenceNumbers[roundRobinIndex]++;
            await SendMessageTo(receivers[roundRobinIndex],message,0);
            roundRobinIndex = (roundRobinIndex+1)%receivers.Count;
        }
    }
}
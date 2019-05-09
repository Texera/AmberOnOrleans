using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public abstract class SingleQueueSendStrategy: SingleQueueBatching, ISendStrategy
    {
        protected List<IWorkerGrain> receivers;
        protected List<ulong> outputSequenceNumbers;
        public SingleQueueSendStrategy(List<IWorkerGrain> receivers, int batchingLimit=1000):base(batchingLimit)
        {
            this.receivers=receivers;
            this.outputSequenceNumbers=Enumerable.Repeat((ulong)0, receivers.Count).ToList();
        }

        public void AddReceiver(IWorkerGrain receiver)
        {
            receivers.Add(receiver);
            this.outputSequenceNumbers.Add(0);
        }

        public void AddReceivers(List<IWorkerGrain> receivers)
        {
            receivers.AddRange(receivers);
            this.outputSequenceNumbers.AddRange(Enumerable.Repeat((ulong)0,receivers.Count));
        }

        public abstract void SendBatchedMessages(IGrain senderIdentifier);

        public abstract void SendEndMessages(IGrain senderIdentifier);

        protected async Task SendMessageTo(IWorkerGrain nextGrain,Immutable<PayloadMessage> message,int retryCount)
        {
            await nextGrain.ReceivePayloadMessage(message).ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                {
                    Console.WriteLine(message.Value.SenderIdentifer.GetType().Name+"("+message.Value.SenderIdentifer+") re-send message with sequence num: "+message.Value.SequenceNumber +" to "+nextGrain+"with retry count "+retryCount);
                    SendMessageTo(nextGrain,message, retryCount + 1);
                }
                else if(retryCount>0)
                {
                    Console.WriteLine(message.Value.SenderIdentifer.GetType().Name+"("+message.Value.SenderIdentifer+") re-send message with sequence num: "+message.Value.SequenceNumber+" to "+nextGrain+" success!");
                }
            });
        }
    }
}
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
    public abstract class MultiQueueSendStrategy: MultiQueueBatching, ISendStrategy
    {
        protected List<IWorkerGrain> receivers;
        protected List<ulong> outputSequenceNumbers;
        public MultiQueueSendStrategy(List<IWorkerGrain> receivers, int batchingLimit=1000):base(receivers.Count,batchingLimit)
        {
            this.receivers=receivers;
            this.outputSequenceNumbers=Enumerable.Repeat((ulong)0, receivers.Count).ToList();
        }

        public abstract void Enqueue(IEnumerable<TexeraTuple> output);

        public abstract void AddReceiver(IWorkerGrain receiver);

        public abstract void AddReceivers(List<IWorkerGrain> receivers);

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
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
        public MultiQueueSendStrategy(int batchingLimit=1000):base(batchingLimit)
        {
            receivers=new List<IWorkerGrain>();
            outputSequenceNumbers=new List<ulong>();
            this.outputSequenceNumbers=Enumerable.Repeat((ulong)0, receivers.Count).ToList();
        }

        public abstract void RemoveAllReceivers();

        public abstract void Enqueue(IEnumerable<TexeraTuple> output);

        public abstract void AddReceiver(IWorkerGrain receiver);

        public abstract void AddReceivers(List<IWorkerGrain> receivers);

        public abstract void SendBatchedMessages(IGrain senderIdentifier);

        public abstract void SendEndMessages(IGrain senderIdentifier);

        protected void SendMessageTo(IWorkerGrain nextGrain,Immutable<PayloadMessage> message,int retryCount)
        {
            nextGrain.ReceivePayloadMessage(message).ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                {
                    string sender,receiver;
                    sender=Utils.GetReadableName(message.Value.SenderIdentifer);
                    receiver=Utils.GetReadableName(nextGrain);
                    Console.WriteLine(sender+" re-send message with sequence num: "+message.Value.SequenceNumber +" to "+receiver+" with retry count "+retryCount);
                    SendMessageTo(nextGrain,message, retryCount + 1);
                }
                else if(retryCount>0)
                {
                    string sender,receiver;
                    sender=Utils.GetReadableName(message.Value.SenderIdentifer);
                    receiver=Utils.GetReadableName(nextGrain);
                    Console.WriteLine(sender+" re-send message with sequence num: "+message.Value.SequenceNumber +" to "+receiver+" successed!");
                }
            });
        }
    }
}
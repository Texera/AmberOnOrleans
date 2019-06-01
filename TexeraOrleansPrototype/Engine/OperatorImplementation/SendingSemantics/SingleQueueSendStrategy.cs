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
        public SingleQueueSendStrategy(int batchingLimit=1000):base(batchingLimit)
        {
            this.receivers=new List<IWorkerGrain>();
            this.outputSequenceNumbers=new List<ulong>();
        }

        public void AddReceiver(IWorkerGrain receiver)
        {
            receivers.Add(receiver);
            this.outputSequenceNumbers.Add(0);
        }

        public void AddReceivers(List<IWorkerGrain> receivers)
        {
            this.receivers.AddRange(receivers);
            this.outputSequenceNumbers.AddRange(Enumerable.Repeat((ulong)0,receivers.Count));
        }


        public void RemoveAllReceivers()
        {
            receivers.Clear();
            this.outputSequenceNumbers.Clear();
        }

        public abstract void SendBatchedMessages(IGrain senderIdentifier);

        public abstract void SendEndMessages(IGrain senderIdentifier);

        protected async Task SendMessageTo(IWorkerGrain nextGrain,Immutable<PayloadMessage> message,int retryCount)
        {
            try
            {
                nextGrain.ReceivePayloadMessage(message).Wait();
            }
            catch(Exception)
            {
                string sender,receiver;
                sender=Utils.GetReadableName(message.Value.SenderIdentifer);
                receiver=Utils.GetReadableName(nextGrain);
                Console.WriteLine(sender+" re-send message with sequence num: "+message.Value.SequenceNumber +" to "+receiver+" with retry count "+retryCount);
                SendMessageTo(nextGrain,message, retryCount + 1);
            }
            //.ContinueWith(async (t)=>
            // {
            // if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
            // {
            //     string sender,receiver;
            //     sender=Utils.GetReadableName(message.Value.SenderIdentifer);
            //     receiver=Utils.GetReadableName(nextGrain);
            //     Console.WriteLine(sender+" re-send message with sequence num: "+message.Value.SequenceNumber +" to "+receiver+" with retry count "+retryCount);
            //     await SendMessageTo(nextGrain,message, retryCount + 1);
            // }
            // else if(retryCount>0)
            // {
            //     string sender,receiver;
            //     sender=Utils.GetReadableName(message.Value.SenderIdentifer);
            //     receiver=Utils.GetReadableName(nextGrain);
            //     Console.WriteLine(sender+" re-send message with sequence num: "+message.Value.SequenceNumber +" to "+receiver+" successed!");
            // }
            // });
            return Task.CompletedTask;
        }
    }
}
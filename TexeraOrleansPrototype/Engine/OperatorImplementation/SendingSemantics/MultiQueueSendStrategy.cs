using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        protected TaskScheduler scheduler;
        public MultiQueueSendStrategy(int batchingLimit=1000):base(batchingLimit)
        {
            receivers=new List<IWorkerGrain>();
            outputSequenceNumbers=new List<ulong>();
            this.outputSequenceNumbers=Enumerable.Repeat((ulong)0, receivers.Count).ToList();
        }

        public void RegisterScheduler(TaskScheduler taskScheduler)
        {
            scheduler=taskScheduler;
        }

        public abstract void RemoveAllReceivers();

        public abstract void Enqueue(IEnumerable<TexeraTuple> output);

        public abstract void AddReceiver(IWorkerGrain receiver);

        public abstract void AddReceivers(List<IWorkerGrain> receivers);

        public abstract Task SendBatchedMessages(IGrain senderIdentifier);

        public abstract Task SendEndMessages(IGrain senderIdentifier);

        protected async Task SendMessageTo(IWorkerGrain nextGrain,Immutable<PayloadMessage> message,int retryCount)
        {

            await Task.Factory.StartNew(async()=>
            {
                try
                {
                    await nextGrain.ReceivePayloadMessage(message);
                }
                catch(TimeoutException)
                {
                    string sender,receiver;
                    sender=Utils.GetReadableName(message.Value.SenderIdentifer);
                    receiver=Utils.GetReadableName(nextGrain);
                    Console.WriteLine(sender+" re-send message with sequence num: "+message.Value.SequenceNumber +" to "+receiver+" with retry count "+retryCount);
                    await SendMessageTo(nextGrain,message, retryCount + 1);
                }
            },CancellationToken.None,TaskCreationOptions.None,scheduler);
            // .ContinueWith(async (t)=>
            // {
            //     if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
            //     {
            //         string sender,receiver;
            //         sender=Utils.GetReadableName(message.Value.SenderIdentifer);
            //         receiver=Utils.GetReadableName(nextGrain);
            //         Console.WriteLine(sender+" re-send message with sequence num: "+message.Value.SequenceNumber +" to "+receiver+" with retry count "+retryCount);
            //         await SendMessageTo(nextGrain,message, retryCount + 1);
            //     }
            //     else if(retryCount>0)
            //     {
            //         string sender,receiver;
            //         sender=Utils.GetReadableName(message.Value.SenderIdentifer);
            //         receiver=Utils.GetReadableName(nextGrain);
            //         Console.WriteLine(sender+" re-send message with sequence num: "+message.Value.SequenceNumber +" to "+receiver+" successed!");
            //     }
            // });
        }
    }
}
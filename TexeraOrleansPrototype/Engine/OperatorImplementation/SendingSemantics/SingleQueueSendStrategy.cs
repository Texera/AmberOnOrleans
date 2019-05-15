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
            receivers.AddRange(receivers);
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
            await nextGrain.ReceivePayloadMessage(message).ContinueWith(async (t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                {
                    string ext1,ext2,opType1,opType2;
                    message.Value.SenderIdentifer.GetPrimaryKey(out ext1);
                    opType1=Utils.GetOperatorTypeFromGrainClass(message.Value.SenderIdentifer.GetType().Name);
                    nextGrain.GetPrimaryKey(out ext2);
                    opType2=Utils.GetOperatorTypeFromGrainClass(nextGrain.GetType().Name);
                    Console.WriteLine(opType1+" "+ext1+" re-send message with sequence num: "+message.Value.SequenceNumber +" to "+opType2+" "+ext2+" with retry count "+retryCount);
                    await SendMessageTo(nextGrain,message, retryCount + 1);
                }
                else if(retryCount>0)
                {
                    string ext1,ext2,opType1,opType2;
                    message.Value.SenderIdentifer.GetPrimaryKey(out ext1);
                    opType1=Utils.GetOperatorTypeFromGrainClass(message.Value.SenderIdentifer.GetType().Name);
                    nextGrain.GetPrimaryKey(out ext2);
                    opType2=Utils.GetOperatorTypeFromGrainClass(nextGrain.GetType().Name);
                    Console.WriteLine(opType1+" "+ext1+" re-send message with sequence num: "+message.Value.SequenceNumber+" to "+opType2+" "+ext2+" success!");
                }
            });
        }
    }
}
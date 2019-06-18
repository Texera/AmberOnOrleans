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
        protected List<SendingUnit> receivers;
        protected List<ulong> outputSequenceNumbers;
        protected TaskScheduler scheduler;
        public MultiQueueSendStrategy(int batchingLimit=1000):base(batchingLimit)
        {
            receivers=new List<SendingUnit>();
            outputSequenceNumbers=new List<ulong>();
            this.outputSequenceNumbers=Enumerable.Repeat((ulong)0, receivers.Count).ToList();
        }

        public void SetPauseFlag(bool flag)
        {
            foreach(SendingUnit unit in receivers)
            {
                unit.SetPauseFlag(flag);
            }
        }

        public void ResumeSending()
        {
            foreach(SendingUnit unit in receivers)
            {
                unit.ResumeSending();
            }
        }
        
        public abstract void RemoveAllReceivers();

        public abstract void Enqueue(List<TexeraTuple> output);

        public abstract void AddReceiver(IWorkerGrain receiver, bool localSending);

        public abstract void AddReceivers(List<IWorkerGrain> receivers, bool localSending);

        public abstract void SendBatchedMessages(IGrain senderIdentifier);

        public abstract void SendEndMessages(IGrain senderIdentifier);


    }
}
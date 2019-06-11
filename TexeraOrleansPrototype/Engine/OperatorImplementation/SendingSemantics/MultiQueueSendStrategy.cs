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
        protected List<FlowControlUnit> receivers;
        protected List<ulong> outputSequenceNumbers;
        protected TaskScheduler scheduler;
        public MultiQueueSendStrategy(int batchingLimit=1000):base(batchingLimit)
        {
            receivers=new List<FlowControlUnit>();
            outputSequenceNumbers=new List<ulong>();
            this.outputSequenceNumbers=Enumerable.Repeat((ulong)0, receivers.Count).ToList();
        }

        public void Pause()
        {
            foreach(SendingUnit unit in receivers)
            {
                unit.Pause();
            }
        }

        public void Resume()
        {
            foreach(SendingUnit unit in receivers)
            {
                unit.Resume();
            }
        }
        public abstract void RemoveAllReceivers();

        public abstract void Enqueue(IEnumerable<TexeraTuple> output);

        public abstract void AddReceiver(IWorkerGrain receiver, bool localSending);

        public abstract void AddReceivers(List<IWorkerGrain> receivers, bool localSending);

        public abstract void SendBatchedMessages(IGrain senderIdentifier);

        public abstract void SendEndMessages(IGrain senderIdentifier);


    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using Orleans.Streams;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public class SendToStream: SingleQueueBatching, ISendStrategy
    {
        protected IAsyncStream<Immutable<PayloadMessage>> stream; 
        private ulong sequenceNumber=0;
        public SendToStream(IAsyncStream<Immutable<PayloadMessage>> stream,int batchingLimit=100):base(batchingLimit)
        {
            this.stream=stream;
        }

        public void AddReceiver(IWorkerGrain receiver, bool localSending)
        {
            throw new NotImplementedException();
        }

        public void AddReceivers(List<IWorkerGrain> receivers, bool localSending)
        {
            throw new NotImplementedException();
        }

        public void RemoveAllReceivers()
        {
            throw new NotImplementedException();
        }

        public async void SendBatchedMessages(IGrain senderIdentifier)
        {
            while(true)
            {
                PayloadMessage message = MakeBatchedMessage(senderIdentifier,sequenceNumber);
                if(message==null)break;
                ++sequenceNumber;
                await stream.OnNextAsync(message.AsImmutable());
            }
        }

        public async void SendEndMessages(IGrain senderIdentifier)
        {
            PayloadMessage message = MakeLastMessage(senderIdentifier,sequenceNumber);
            if(message!=null)
            {
                ++sequenceNumber;
                await stream.OnNextAsync(message.AsImmutable());
            }
            message = new PayloadMessage(senderIdentifier,sequenceNumber++,null,true);
            await stream.OnNextAsync(message.AsImmutable());
        }

        public void Pause()
        {

        }

        public void Resume()
        {
            
        }

    }
}
using Orleans;
using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using System;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public interface ISendStrategy
    {
        void Enqueue(List<TexeraTuple> output);
        void SendBatchedMessages(IGrain senderIdentifier);
        void SendEndMessages(IGrain senderIdentifier);
        void AddReceiver(IWorkerGrain receiver,bool localSending=false);
        void AddReceivers(List<IWorkerGrain> receivers,bool localSending=false);
        void RemoveAllReceivers();
        void SetPauseFlag(bool flag);
        void ResumeSending();
        List<IWorkerGrain> GetReceivers();
        
    }
}
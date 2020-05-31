using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public class OneToOne: SingleQueueBatching, ISendStrategy
    {

        private SendingUnit unit;
        private ulong seqNum = 0;
        public OneToOne(int batchingLimit=1000):base(batchingLimit)
        {
        }

        public void AddReceiver(IWorkerGrain receiver, bool localSending = false)
        {
            if(localSending)
            {
                unit = new SendingUnit(receiver);
            }
            else
                unit = new FlowControlUnit(receiver);
        }

        public void AddReceivers(List<IWorkerGrain> receivers, bool localSending = false)
        {
            throw new NotImplementedException();
        }

        public List<IWorkerGrain> GetReceivers()
        {
            return new List<IWorkerGrain>{unit.receiver};
        }

        public void RemoveAllReceivers()
        {
            throw new NotImplementedException();
        }

        public void ResumeSending()
        {
            unit.ResumeSending();
        }

        public void SendBatchedMessages(IGrain senderIdentifier)
        {
            while(true)
            {
                PayloadMessage message = MakeBatchedMessage(senderIdentifier,seqNum);
                if(message==null)
                {
                    break;
                }
                seqNum++;
                unit.Send(message);
            }
        }

        public void SendEndMessages(IGrain senderIdentifier)
        {
            PayloadMessage message=MakeLastMessage(senderIdentifier,seqNum);
            if(message!=null)
            {
                seqNum++;
                unit.Send(message);
            }
            message = new PayloadMessage(senderIdentifier,seqNum,null,true);
            unit.Send(message);
        }

        public void SetPauseFlag(bool flag)
        {
            unit.SetPauseFlag(flag);
        }

        public override string ToString()
        {
            return "OneToOne: to "+Utils.GetReadableName(unit.receiver);
        }
    }
}
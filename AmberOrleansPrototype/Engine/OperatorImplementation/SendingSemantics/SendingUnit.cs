using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Engine;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public class SendingUnit
    {
        public IWorkerGrain receiver;

        public virtual void SetPauseFlag(bool flag)
        {

        }

        public virtual void ResumeSending()
        {

        }

        public SendingUnit(IWorkerGrain receiver)
        {
            this.receiver=receiver;
        }

        public virtual void Send(PayloadMessage message)
        {
            SendInternal(message.AsImmutable(),0);
        }


        private void SendInternal(Immutable<PayloadMessage> message,int retryCount)
        {
            receiver.ReceivePayloadMessage(message).ContinueWith((t)=>
            {
                if(Utils.IsTaskFaultedAndStillNeedRetry(t,retryCount))
                {
                    SendInternal(message,retryCount+1);
                }
            });
        }
    }
}
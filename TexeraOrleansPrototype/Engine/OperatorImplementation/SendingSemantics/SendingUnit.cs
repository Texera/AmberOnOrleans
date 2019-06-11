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
        protected IWorkerGrain receiver;

        public virtual void Pause()
        {

        }

        public virtual void Resume()
        {
            
        }

        public SendingUnit(IWorkerGrain receiver)
        {
            this.receiver=receiver;
        }

        public virtual void Send(Immutable<PayloadMessage> message)
        {
            //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver));
            SendInternal(message,0);
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
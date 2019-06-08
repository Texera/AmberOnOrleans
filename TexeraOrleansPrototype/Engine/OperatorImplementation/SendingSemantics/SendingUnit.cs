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

        public SendingUnit(IWorkerGrain receiver)
        {
            this.receiver=receiver;
        }

        public virtual void Send(Immutable<PayloadMessage> message)
        {
            //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver));
            receiver.ReceivePayloadMessage(message);
        }
    }
}
using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.MessagingSemantics
{
    public interface IOrderingEnforcer
    {
        bool PreProcess(Immutable<PayloadMessage> message);
        List<ControlMessage.ControlMessageType> PreProcess(Immutable<ControlMessage> message);
        void IndeedReceivePayloadMessage(string sender);
        void CheckStashed(ref List<TexeraTuple> batchList, ref bool isEnd, string sender);
        void CheckStashed(ref List<ControlMessage.ControlMessageType> controlMessages, string sender);
    }
}
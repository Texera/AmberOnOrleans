using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine
{
    public class PayloadMessage: TexeraMessage
    {
        public PayloadMessage(string sender, ulong sequenceNumber, List<TexeraTuple> payload,bool isEnd):base(sender, sequenceNumber)
        {
            Payload=payload;
            IsEnd=isEnd;
        }
        public bool IsEnd;
        public List<TexeraTuple> Payload;
    }    
}
using System.Collections.Generic;
using Engine.Common;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine
{
    public class TexeraMessage
    {
        public TexeraMessage(string sender,ulong sequenceNumber)
        {
            SequenceNumber=sequenceNumber;
            SenderIdentifer=sender;
        }
        public ulong SequenceNumber;
        public string SenderIdentifer;
    }    
}
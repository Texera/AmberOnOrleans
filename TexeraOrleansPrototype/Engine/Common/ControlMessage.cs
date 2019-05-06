using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine
{
    public class ControlMessage: TexeraMessage
    {
        public ControlMessage(IGrain sender, ulong sequenceNumber, ControlMessageType type):base(sender,sequenceNumber)
        {
            Type=type;
        }
        public enum ControlMessageType
        {
            Pause,
            Resume,
            Start,
            Deactivate,
        }
        public ControlMessageType Type;
    }    
}
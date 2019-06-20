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
        public ControlMessage(IGrain sender, ulong sequenceNumber, ControlMessageType type,object additionalInfo=null):base(sender,sequenceNumber)
        {
            Type=type;
            AdditionalInfo=additionalInfo;
        }
        public enum ControlMessageType
        {
            Pause,
            Resume,
            Start,
            Deactivate,
            addCallbackWorker,
        }
        public ControlMessageType Type;
        public object AdditionalInfo;
    }    
}
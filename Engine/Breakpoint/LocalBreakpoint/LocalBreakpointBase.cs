using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;

namespace Engine.Breakpoint.LocalBreakpoint
{
    public abstract class LocalBreakpointBase
    {
        public string id;
        public ulong version;
        public bool isReported = false;
        public LocalBreakpointBase(string id, ulong version)
        {
            this.id = id;
            this.version = version;
        }

        public abstract void Accept(TexeraTuple tuple);

        public abstract bool IsDirty
        {
            get;
        }

        public abstract bool IsTriggered
        {
            get;
        }
        
        public abstract bool IsFaultedTuple
        {
            get;
        }

    }

}
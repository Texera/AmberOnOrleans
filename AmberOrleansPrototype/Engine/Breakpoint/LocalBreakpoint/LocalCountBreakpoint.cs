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
    public class LocalCountBreakpoint:LocalBreakpointBase
    {
        public ulong current;
        private ulong target;
        public LocalCountBreakpoint(string id, ulong version, ulong target):base(id,version)
        {
            this.target = target;
            this.current = 0;
        }

        public override void Accept(TexeraTuple tuple){
            this.current++;
        }

        public override bool IsDirty => isReported;

        public override bool IsTriggered => current == target;

        public override bool IsFaultedTuple => false;

    }

}
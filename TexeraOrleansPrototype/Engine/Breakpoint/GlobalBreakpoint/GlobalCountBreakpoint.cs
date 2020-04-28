using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using System.Linq;
using TexeraUtilities;
using Engine.Breakpoint.LocalBreakpoint;
using Engine.OperatorImplementation.Common;

namespace Engine.Breakpoint.GlobalBreakpoint
{
    public class GlobalCountBreakpoint : GlobalBreakpointBase
    {
        private ulong target;
        private ulong current;
        public GlobalCountBreakpoint(string id, ulong target):base(id)
        {
            this.target = target;
            this.current = 0;
        }

        public override bool isTriggered => current==target;

        public override bool IsCompleted => isTriggered;

        public override string Report()
        {
            return $"GlobalCountBreakpoint {id} triggered! current count = {current} target count = {target}";
        }

        protected override void AcceptImpl(IWorkerGrain sender, LocalBreakpointBase breakpoint)
        {
            current += ((LocalCountBreakpoint)breakpoint).current;
        }

        protected async override Task<List<IWorkerGrain>> PartitionImpl(List<IWorkerGrain> layer)
        {
            ulong remaining = target-current;
            ulong currentSum = 0L;
            int length = layer.Count;
            ulong partition = remaining/(ulong)length;
            if(partition > 0)
            {
                for(int i=0; i < length-1; ++i)
                {
                   await layer[i].AddBreakpoint(new LocalCountBreakpoint(id,version,partition)); 
                   currentSum+=partition;
                }
                await layer[length-1].AddBreakpoint(new LocalCountBreakpoint(id,version,remaining-currentSum));
                return layer;
            }
            else
            {
                var idx = new Random().Next(0,length);
                await layer[idx].AddBreakpoint(new LocalCountBreakpoint(id,version,remaining));
                return new List<IWorkerGrain>{layer[idx]};
            }
        }
    }
}
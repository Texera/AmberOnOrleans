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
    public abstract class GlobalBreakpointBase
    {
        protected string id;
        protected HashSet<IWorkerGrain> unReportedWorkers;
        protected HashSet<IWorkerGrain> allWorkers;
        protected ulong version;

        public GlobalBreakpointBase(string id)
        {
            this.id = id;
        }

        public bool Accept(IWorkerGrain sender,LocalBreakpointBase breakpoint)
        {
            if(breakpoint.version == version && unReportedWorkers.Contains(sender))
            {
                unReportedWorkers.Remove(sender);
                AcceptImpl(sender,breakpoint);
                return true;
            }
            return false;
        }

        protected abstract void AcceptImpl(IWorkerGrain sender,LocalBreakpointBase breakpoint);

        public abstract bool isTriggered
        {
            get;
        }

        public async void Partition(List<IWorkerGrain> layer)
        {
            version++;
            List<IWorkerGrain> assgined = await PartitionImpl(layer);
            HashSet<IWorkerGrain> assginedSet = assgined.ToHashSet();
            foreach(IWorkerGrain worker in allWorkers.Where(x => !assginedSet.Contains(x)))
            {
                await worker.RemoveBreakpoint(id);
            }
            unReportedWorkers.Clear();
            allWorkers.Clear();
            allWorkers = new HashSet<IWorkerGrain>(assginedSet);
            unReportedWorkers = new HashSet<IWorkerGrain>(assginedSet);

        }

        protected abstract Task<List<IWorkerGrain>> PartitionImpl(List<IWorkerGrain> layer);

        public virtual bool IsRepartitionRequired
        {
            get
            {
                return unReportedWorkers.Count == 0;
            }
        }

        public abstract string Report();

        public abstract bool IsCompleted
        {
            get;
        }

        public virtual bool NeedCollecting
        {
            get
            {
                return unReportedWorkers.Count > 0;
            }
        }

        public async Task Collect()
        {
            //TODO: Assume realiable message sending for simplicity now.
            //should only call it once
            foreach(IWorkerGrain grain in unReportedWorkers)
            {
                Accept(grain, await grain.QueryBreakpoint(id));
            }
            unReportedWorkers.Clear();
        }

        public async Task Remove()
        {
            //TODO: Assume realiable message sending for simplicity now.
            //should only call it once
            foreach(IWorkerGrain grain in allWorkers)
            {
                await grain.RemoveBreakpoint(id);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;
using Orleans;
using Engine.LinkSemantics;
using Engine.Breakpoint.GlobalBreakpoint;
using Engine.DeploySemantics;

namespace Engine.OperatorImplementation.Common
{
    public abstract class Operator
    {   
        public abstract Pair<List<WorkerLayer>,List<LinkStrategy>> GenerateTopology();

        public abstract void AssignBreakpoint(List<WorkerLayer> layers, Dictionary<IWorkerGrain,WorkerState> states, GlobalBreakpointBase breakpoint);

        public abstract bool IsStaged(Operator from);

        public abstract string GetHashFunctionAsString(Guid from);
    }
}
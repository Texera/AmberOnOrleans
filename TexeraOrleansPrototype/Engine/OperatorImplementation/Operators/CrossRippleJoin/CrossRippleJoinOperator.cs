using System;
using System.Collections.Generic;
using Engine.Breakpoint.GlobalBreakpoint;
using Engine.DeploySemantics;
using Engine.LinkSemantics;
using Engine.OperatorImplementation.Common;
using Orleans;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{
    public class CrossRippleJoinOperator : Operator
    {
        private Guid innerGuid;
        private int innerIndex;
        private int outerIndex;


        public CrossRippleJoinOperator(int innerIndex, int outerIndex, Guid innerGuid)
        {
            this.innerGuid = innerGuid;
            this.outerIndex = outerIndex;
            this.innerIndex = innerIndex;
        }


        public override void AssignBreakpoint(List<WorkerLayer> layers, Dictionary<IWorkerGrain, WorkerState> states, GlobalBreakpointBase breakpoint)
        {
            throw new System.NotImplementedException();
        }

        public override Pair<List<WorkerLayer>, List<LinkStrategy>> GenerateTopology()
        {
            return new Pair<List<WorkerLayer>,List<LinkStrategy>>
            (
                new List<WorkerLayer>
                {
                    new ProcessorWorkerLayer("cross_ripple_join.main",1,(i)=>new CrossRippleJoinProcessor(innerIndex,outerIndex,innerGuid),null)
                },
                new List<LinkStrategy>
                {

                }
            );
        }

        public override string GetHashFunctionAsString(Guid from)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsStaged(Operator from)
        {
            return true;
        }
    }
}
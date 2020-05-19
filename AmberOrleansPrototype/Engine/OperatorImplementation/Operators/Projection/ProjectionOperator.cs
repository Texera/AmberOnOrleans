using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Breakpoint.GlobalBreakpoint;
using Engine.DeploySemantics;
using Engine.LinkSemantics;
using Engine.OperatorImplementation.Common;
using Orleans;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{
    public class ProjectionOperator : Operator
    {
        public List<int> ProjectionIndexs;
        public ProjectionOperator(List<int> projectionIndexs)
        {
            this.ProjectionIndexs = projectionIndexs;
        }

        public override void AssignBreakpoint(List<WorkerLayer> layers, Dictionary<IWorkerGrain, WorkerState> states, GlobalBreakpointBase breakpoint)
        {
            breakpoint.Partition(layers[0].Layer.Values.SelectMany(x=>x).Where(x => states[x]!=WorkerState.Completed).ToList());
        }

        public override Pair<List<WorkerLayer>, List<LinkStrategy>> GenerateTopology()
        {
            return new Pair<List<WorkerLayer>,List<LinkStrategy>>
            (
                new List<WorkerLayer>
                {
                    new ProcessorWorkerLayer("projection.main",Constants.DefaultNumGrainsInOneLayer,(i)=>new ProjectionProcessor(ProjectionIndexs),null)
                },
                new List<LinkStrategy>
                {

                }
            );
        }

        public override string GetHashFunctionAsString(Guid from)
        {
            throw new NotImplementedException();
        }

        public override bool IsStaged(Operator from)
        {
            return true;
        }
    }
}
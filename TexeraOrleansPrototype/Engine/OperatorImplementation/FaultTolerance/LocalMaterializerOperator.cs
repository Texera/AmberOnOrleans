using Engine.Breakpoint.GlobalBreakpoint;
using Engine.DeploySemantics;
using Engine.LinkSemantics;
using Engine.OperatorImplementation.Common;
using Orleans;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TexeraUtilities;

namespace Engine.OperatorImplementation.FaultTolerance
{
    public class LocalMaterializerOperator : Operator
    {
        private Guid id;
        public LocalMaterializerOperator(Guid id) : base()
        {
            this.id = id;
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
                    new ProcessorWorkerLayer("local_materializer.main",Constants.DefaultNumGrainsInOneLayer,(i)=>new LocalMaterializer(id,i),null)
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
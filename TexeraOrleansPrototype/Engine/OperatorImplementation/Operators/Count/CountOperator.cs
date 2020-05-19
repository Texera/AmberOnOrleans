using Engine.Breakpoint.GlobalBreakpoint;
using Engine.DeploySemantics;
using Engine.LinkSemantics;
using Engine.OperatorImplementation.Common;
using Orleans;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{
    public class CountOperator : Operator
    {
        public CountOperator() : base()
        {
            
        }

        public override void AssignBreakpoint(List<WorkerLayer> layers, Dictionary<IWorkerGrain, WorkerState> states, GlobalBreakpointBase breakpoint)
        {
            throw new System.NotImplementedException();
        }

        public override Pair<List<WorkerLayer>, List<LinkStrategy>> GenerateTopology()
        {

            var firstLayer = new ProcessorWorkerLayer("count.main",Constants.DefaultNumGrainsInOneLayer,(i)=>new CountProcessor(),null);
            var secondLayer = new ProcessorWorkerLayer("count.final",1,(i)=>new CountFinalProcessor(),null);

            return new Pair<List<WorkerLayer>,List<LinkStrategy>>
            (
                new List<WorkerLayer>
                {
                    firstLayer,
                    secondLayer
                },
                new List<LinkStrategy>
                {
                    new AllToOneLinking(firstLayer,secondLayer,Constants.BatchSize)
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
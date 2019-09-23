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
    public class SentimentAnalysisOperator : Operator
    {
        public int PredictIndex;

        public SentimentAnalysisOperator(int predictIndex)
        {
            PredictIndex=predictIndex;
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
                    new ProcessorWorkerLayer("sentiment_analysis.main",Constants.DefaultNumGrainsInOneLayer,(i)=>new SentimentAnalysisProcessor(PredictIndex),null)
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
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
    public class KeywordOperator : Operator
    {
        public int SearchIndex;
        public string Query;
        public KeywordOperator(int searchIndex, string query)
        {
            if(query == null)
            {
                query = "";
            }
            this.Query = query;
            this.SearchIndex=searchIndex;
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
                    new ProcessorWorkerLayer("keyword_search.main",Constants.DefaultNumGrainsInOneLayer,(i)=>new KeywordSearchProcessor(SearchIndex,Query),null)
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
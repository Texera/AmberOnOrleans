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
    public enum FilterType
    {
        Equal,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        NotEqual,
    }

    public class FilterOperator<T> : Operator where T : IComparable<T>
    {

        public int FilterIndex;
        public string Threshold;
        public FilterType Type;

        public FilterOperator(int filterIndex, string threshold, string type)
        {
            this.Threshold = threshold;
            this.FilterIndex = filterIndex;
            switch(type)
            {
                case "=":
                    this.Type=FilterType.Equal;
                    break;
                case ">":
                    this.Type=FilterType.Greater;
                    break;
                case ">=":
                    this.Type=FilterType.GreaterOrEqual;
                    break;
                case "<":
                    this.Type=FilterType.Less;
                    break;
                case "<=":
                    this.Type=FilterType.LessOrEqual;
                    break;
                case "≠":
                    this.Type=FilterType.NotEqual;
                    break;
                default:
                    this.Type=FilterType.Equal;
                    break;
            }
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
                    new ProcessorWorkerLayer("filter.main",Constants.DefaultNumGrainsInOneLayer,(i)=>new FilterProcessor<T>(FilterIndex,Type,Threshold),null)
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
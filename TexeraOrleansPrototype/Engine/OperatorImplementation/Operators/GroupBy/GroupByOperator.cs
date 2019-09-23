using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Engine.Breakpoint.GlobalBreakpoint;
using Engine.DeploySemantics;
using Engine.LinkSemantics;
using Engine.OperatorImplementation.Common;
using Orleans;
using Serialize.Linq.Serializers;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{
    public enum AggregationType
    {
        Count,
        Max,
        Min,
        Average,
        Sum
    }

    public class GroupByOperator : Operator
    {
        public int GroupByIndex;
        public int AggregationIndex;
        public AggregationType Aggregation;

        public GroupByOperator(int groupByIndex,int aggregationIndex, string aggregationFunction)
        {
            this.GroupByIndex=groupByIndex;
            this.AggregationIndex=aggregationIndex;
            switch(aggregationFunction)
            {
                case "max":
                    this.Aggregation=AggregationType.Max;
                    break;
                case "min":
                    this.Aggregation=AggregationType.Min;
                    break;
                case "sum":
                    this.Aggregation=AggregationType.Sum;
                    break;
                case "avg":
                    this.Aggregation=AggregationType.Average;
                    break;
                case "count":
                    this.Aggregation=AggregationType.Count;
                    break;
                default:
                    this.Aggregation=AggregationType.Count;
                    break;
            }
        }

        public override void AssignBreakpoint(List<WorkerLayer> layers, Dictionary<IWorkerGrain, WorkerState> states, GlobalBreakpointBase breakpoint)
        {
            throw new System.NotImplementedException();
        }

        public override Pair<List<WorkerLayer>, List<LinkStrategy>> GenerateTopology()
        {
            var firstLayer = new ProcessorWorkerLayer("groupby.main",Constants.DefaultNumGrainsInOneLayer,(i)=>new GroupByProcessor(GroupByIndex,Aggregation,AggregationIndex),null);
            var secondLayer = new ProcessorWorkerLayer("groupby.final",Constants.DefaultNumGrainsInOneLayer,(i)=>new GroupByFinalProcessor(Aggregation),null);
            Expression<Func<TexeraTuple,int>> exp=tuple=>tuple.FieldList[0].GetStableHashCode();
            var serializer = new ExpressionSerializer(new JsonSerializer());
            var hashFunc = serializer.SerializeText(exp);

            return new Pair<List<WorkerLayer>,List<LinkStrategy>>
            (
                new List<WorkerLayer>
                {
                    firstLayer,
                    secondLayer
                },
                new List<LinkStrategy>
                {
                    new HashBasedShuffleLinking(hashFunc,firstLayer,secondLayer,Constants.BatchSize)
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
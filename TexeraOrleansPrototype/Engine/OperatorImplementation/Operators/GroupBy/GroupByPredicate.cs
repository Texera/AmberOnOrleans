using System.Collections.Generic;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class GroupByPredicate : PredicateBase
    {
        public int GroupByIndex;
        public int AggregationIndex;
        public AggregationType Aggregation;

        public enum AggregationType
        {
            Count,
            Max,
            Min,
            Average,
            Sum
        }



        public GroupByPredicate(int groupByIndex,int aggregationIndex, string aggregationFunction,int batchingLimit=1000):base(batchingLimit)
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
    }
}
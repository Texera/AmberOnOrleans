using System.Collections.Generic;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class GroupByPredicate : PredicateBase
    {
        public int GroupByIndex;
        public int AggregationIndex;
        public string AggregationFunction;
        public GroupByPredicate(int groupByIndex,int aggregationIndex, string aggregationFunction,int batchingLimit=1000):base(batchingLimit)
        {
            this.GroupByIndex=groupByIndex;
            this.AggregationFunction=aggregationFunction;
            this.AggregationIndex=aggregationIndex;
        }
    }
}
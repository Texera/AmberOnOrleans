using System;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class HashJoinPredicate : PredicateBase
    {
        public int InnerTableIndex;
        public int OuterTableIndex;
        public Guid InnerTableID=Guid.Empty;
        public Guid outerTableID=Guid.Empty;
        public HashJoinPredicate(int innerTableIndex,int outerTableIndex,int batchingLimit=1000):base(batchingLimit)
        {
            InnerTableIndex=innerTableIndex; 
            OuterTableIndex=outerTableIndex;
        }

        public override void WhenAddInOperator(Operator operatorToAdd)
        {
            if(((ScanPredicate)operatorToAdd.Predicate).File.EndsWith("customer.tbl"))
                InnerTableID=operatorToAdd.OperatorGuid;
            else
                outerTableID=operatorToAdd.OperatorGuid;
        }
    }
}
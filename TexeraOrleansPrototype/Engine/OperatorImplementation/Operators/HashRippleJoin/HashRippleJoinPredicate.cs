using System;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class HashRippleJoinPredicate : PredicateBase
    {
        public int InnerTableIndex;
        public int OuterTableIndex;
        public Guid InnerTableID=Guid.Empty;
        public Guid outerTableID=Guid.Empty;
        public int TableID;
        public HashRippleJoinPredicate(int innerTableIndex,int outerTableIndex, int tableID,int batchingLimit=1000):base(batchingLimit)
        {
            InnerTableIndex=innerTableIndex; 
            OuterTableIndex=outerTableIndex;
            TableID=tableID;
        }

        public override void WhenAddInOperator(Operator operatorToAdd)
        {
            if(InnerTableID==Guid.Empty)
                InnerTableID=operatorToAdd.OperatorGuid;
            else
                outerTableID=operatorToAdd.OperatorGuid;
        }
    }
}
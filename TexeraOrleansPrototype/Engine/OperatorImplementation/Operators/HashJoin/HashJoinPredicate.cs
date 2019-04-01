using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class HashJoinPredicate : PredicateBase
    {
        public int JoinFieldIndex;

        public int TableID;

        public HashJoinPredicate(int joinFieldIndex, int tableID)
        {
            JoinFieldIndex=joinFieldIndex;    
            TableID=tableID;
        }
    }
}
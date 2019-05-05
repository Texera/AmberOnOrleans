using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class HashRippleJoinPredicate : PredicateBase
    {
        public int JoinFieldIndex;
        public int TableID;
        public HashRippleJoinPredicate(int joinFieldIndex, int tableID,int batchingLimit=1000):base(batchingLimit)
        {
            JoinFieldIndex=joinFieldIndex;    
            TableID=tableID;
        }
    }
}
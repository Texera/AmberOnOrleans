using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class HashJoinPredicate : PredicateBase
    {
        public int JoinFieldIndex;
        public int TableID;
        public HashJoinPredicate(int joinFieldIndex, int tableID,int outputLimitPerBatch=-1,int batchingLimit=1000,int timeLimitPerBatch=-1):base(outputLimitPerBatch,batchingLimit,timeLimitPerBatch)
        {
            JoinFieldIndex=joinFieldIndex;    
            TableID=tableID;
        }
    }
}
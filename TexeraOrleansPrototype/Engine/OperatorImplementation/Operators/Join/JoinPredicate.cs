using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class JoinPredicate : PredicateBase
    {

        public int TableID;
        public JoinPredicate(int tableID,int outputLimitPerBatch=-1,int batchingLimit=1000,int timeLimitPerBatch=-1):base(outputLimitPerBatch,batchingLimit,timeLimitPerBatch)
        {
            this.TableID=tableID;
        }
    }
}
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class CrossRippleJoinPredicate : PredicateBase
    {

        public int TableID;
        public CrossRippleJoinPredicate(int tableID,int batchingLimit=1000):base(batchingLimit)
        {
            this.TableID=tableID;
        }
    }
}
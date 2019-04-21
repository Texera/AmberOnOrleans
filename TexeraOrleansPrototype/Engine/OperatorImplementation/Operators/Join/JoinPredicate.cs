using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class JoinPredicate : PredicateBase
    {

        public int TableID;
        public JoinPredicate(int tableID,int batchingLimit=1000):base(batchingLimit)
        {
            this.TableID=tableID;
        }
    }
}
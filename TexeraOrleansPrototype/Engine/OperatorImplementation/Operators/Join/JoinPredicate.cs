using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class JoinPredicate : PredicateBase
    {

        public int TableID;
        public JoinPredicate(int tableID)
        {
            this.TableID=tableID;
        }
    }
}
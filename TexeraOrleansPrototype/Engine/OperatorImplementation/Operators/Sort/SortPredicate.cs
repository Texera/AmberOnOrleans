using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class SortPredicate : PredicateBase
    {
        public int SortIndex;
        public SortPredicate(int sortIndex,int batchingLimit=1000):base(batchingLimit)
        {
            this.SortIndex=sortIndex;
        }
    }
}
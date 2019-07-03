using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class CrossRippleJoinPredicate : PredicateBase
    {

        public CrossRippleJoinPredicate(int batchingLimit=1000):base(batchingLimit)
        {
        }
    }
}
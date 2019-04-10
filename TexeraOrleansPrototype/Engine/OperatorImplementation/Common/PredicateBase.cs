

namespace Engine.OperatorImplementation.Common
{
    public abstract class PredicateBase : IPredicate
    {
        public int BatchingLimit;

        public PredicateBase(int batchingLimit=1000)
        {
            BatchingLimit=batchingLimit;
        }
    }
}


namespace Engine.OperatorImplementation.Common
{
    public abstract class PredicateBase : IPredicate
    {
        public int BatchingLimit;

        public PredicateBase(int batchingLimit=5000)
        {
            BatchingLimit=batchingLimit;
        }

        public virtual void WhenAddInOperator(Operator operatorToAdd)
        {

        }

        public virtual void WhenAddOutOperator(Operator operatorToAdd)
        {
            
        }
    }
}
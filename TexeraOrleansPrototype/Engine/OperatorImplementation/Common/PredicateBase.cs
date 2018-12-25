

namespace Engine.OperatorImplementation.Common
{
    public abstract class PredicateBase : IPredicate
    {
        public abstract Operator GetNewOperator(int numberOfGrains);
    }
}
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class CountPredicate : PredicateBase
    {
        public CountPredicate()
        {
        }

        public override Operator GetNewOperator(int numberOfGrains)
        {
            return new CountOperator(this, numberOfGrains);
        }
    }
}
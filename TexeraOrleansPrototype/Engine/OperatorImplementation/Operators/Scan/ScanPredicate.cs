using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanPredicate : PredicateBase
    {
        public ScanPredicate()
        {
        }

        public override Operator GetNewOperator(int numberOfGrains)
        {
            return new ScanOperator(this, numberOfGrains);
        }
    }
}
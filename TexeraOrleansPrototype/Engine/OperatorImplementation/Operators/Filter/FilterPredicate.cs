using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class FilterPredicate : PredicateBase
    {
        private int threshold;

        public FilterPredicate(int threshold)
        {
            this.threshold = threshold;
        }

        public int GetThreshold()
        {
            return threshold;
        }

        public override Operator GetNewOperator(int numberOfGrains)
        {
            return new FilterOperator(this, numberOfGrains);
        }
    }
}
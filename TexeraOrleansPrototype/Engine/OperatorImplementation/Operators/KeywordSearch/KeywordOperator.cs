using Engine.Common;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class KeywordOperator : Operator
    {
        public KeywordOperator(KeywordPredicate predicate, int numberOfGrains) : base(predicate, numberOfGrains)
        {
        }
    }
}
using Engine.Common;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class FilterOperator : Operator
    {
        public FilterOperator(FilterPredicate predicate, int numberOfGrains) : base(predicate, numberOfGrains)
        {
        }
        
    }
}
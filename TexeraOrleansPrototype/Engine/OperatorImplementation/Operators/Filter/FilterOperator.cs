using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class FilterOperator : Operator
    {
        public FilterOperator(FilterPredicate predicate,IGrainFactory factory) : base(predicate)
        {
            PrincipalGrain = factory.GetGrain<IPrincipalGrain>(OperatorGuid,"Principal");
        }
        
    }
}
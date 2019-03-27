using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class FilterOperator : Operator
    {
        public FilterOperator(FilterPredicate predicate) : base(predicate)
        {

        }
        public override void SetUpPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IFilterPrincipalGrain>(OperatorGuid);
        }
    }
}
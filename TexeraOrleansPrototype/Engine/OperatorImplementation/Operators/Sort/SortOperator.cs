using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class SortOperator : Operator
    {
        public SortOperator(SortPredicate predicate) : base(predicate)
        {

        }
        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<ISortPrincipalGrain>(OperatorGuid);
        }
    }
}
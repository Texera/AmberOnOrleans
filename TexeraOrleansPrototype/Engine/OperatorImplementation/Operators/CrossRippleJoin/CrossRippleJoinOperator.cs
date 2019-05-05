using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class CrossRippleJoinOperator : Operator
    {
        public CrossRippleJoinOperator(CrossRippleJoinPredicate predicate) : base(predicate)
        {

        }
        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<ICrossRippleJoinPrincipalGrain>(OperatorGuid);
        }
    }
}
using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class JoinOperator : Operator
    {
        public JoinOperator(FilterPredicate predicate) : base(predicate)
        {

        }
        public override void SetUpPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IJoinPrincipalGrain>(OperatorGuid);
        }
    }
}
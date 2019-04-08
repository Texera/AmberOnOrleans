using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class JoinOperator : Operator
    {
        public JoinOperator(JoinPredicate predicate) : base(predicate)
        {

        }
        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IJoinPrincipalGrain>(OperatorGuid);
        }
    }
}
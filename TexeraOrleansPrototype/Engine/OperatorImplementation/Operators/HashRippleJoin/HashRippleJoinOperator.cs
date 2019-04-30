using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class HashRippleJoinOperator : Operator
    {
        public HashRippleJoinOperator(HashRippleJoinPredicate predicate) : base(predicate)
        {

        }
        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IHashRippleJoinPrincipalGrain>(OperatorGuid);
        }
    }
}
using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class HashJoinOperator : Operator
    {
        public HashJoinOperator(HashJoinPredicate predicate) : base(predicate)
        {

        }
        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IHashJoinPrincipalGrain>(OperatorGuid);
        }
    }
}
using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class KeywordOperator : Operator
    {
        public KeywordOperator(KeywordPredicate predicate) : base(predicate)
        {
            
        }

        public override void SetUpPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IPrincipalGrain>(OperatorGuid,"Principal");
        }
    }
}
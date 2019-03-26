using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class KeywordOperator : Operator
    {
        public KeywordOperator(KeywordPredicate predicate,IGrainFactory factory) : base(predicate)
        {
            PrincipalGrain = factory.GetGrain<IPrincipalGrain>(OperatorGuid,"Principal");
        }
    }
}
using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanOperator : Operator
    {
        public ScanOperator(ScanPredicate predicate) : base(predicate,true)
        {
        }

        public override void SetUpPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IScanPrincipalGrain>(OperatorGuid,"Principal");
        }
    }
}
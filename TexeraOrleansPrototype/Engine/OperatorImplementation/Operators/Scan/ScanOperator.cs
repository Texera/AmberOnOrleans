using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanOperator : Operator
    {
        public ScanOperator(ScanPredicate predicate,IGrainFactory factory) : base(predicate,true)
        {
            PrincipalGrain = factory.GetGrain<IScanPrincipalGrain>(OperatorGuid,"Principal");
        }
    }
}
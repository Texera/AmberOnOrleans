using Engine.OperatorImplementation.Common;
using Orleans;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Engine.OperatorImplementation.Operators
{
    public class CountOperator : Operator
    {
        public CountOperator(CountPredicate predicate) : base(predicate)
        {
            
        }

        public override void SetUpPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IPrincipalGrain>(OperatorGuid,"Principal");
        }
    }
}
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

        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<ICountPrincipalGrain>(OperatorGuid);
        }
    }
}
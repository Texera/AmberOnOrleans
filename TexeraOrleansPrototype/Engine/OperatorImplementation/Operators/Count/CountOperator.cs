using Engine.OperatorImplementation.Common;
using Orleans;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Engine.OperatorImplementation.Operators
{
    public class CountOperator : Operator
    {
        public CountOperator(CountPredicate predicate,IGrainFactory factory) : base(predicate)
        {
            PrincipalGrain = factory.GetGrain<IPrincipalGrain>(OperatorGuid,"Principal");
        }
    }
}
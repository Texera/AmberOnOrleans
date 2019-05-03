using System;
using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class SortOperator<T> : Operator where T:IComparable<T>
    {
        public SortOperator(SortPredicate predicate) : base(predicate)
        {

        }
        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<ISortPrincipalGrain<T>>(OperatorGuid);
        }
    }
}
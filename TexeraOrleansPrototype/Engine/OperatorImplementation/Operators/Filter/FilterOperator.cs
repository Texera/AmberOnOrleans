using System;
using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class FilterOperator<T> : Operator where T:IComparable<T>
    {
        public FilterOperator(FilterPredicate predicate) : base(predicate)
        {

        }
        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IFilterPrincipalGrain<T>>(OperatorGuid);
        }
    }
}
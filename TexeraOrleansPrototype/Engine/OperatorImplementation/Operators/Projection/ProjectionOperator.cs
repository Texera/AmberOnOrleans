using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class ProjectionOperator : Operator
    {
        
        public ProjectionOperator(KeywordPredicate predicate) : base(predicate)
        {
            
        }

        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IProjectionPrincipalGrain>(OperatorGuid);
        }
    }
}
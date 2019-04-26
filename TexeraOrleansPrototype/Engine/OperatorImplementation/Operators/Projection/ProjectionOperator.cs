using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class ProjectionOperator : Operator
    {
        
        public ProjectionOperator(ProjectionPredicate predicate) : base(predicate)
        {
            
        }

        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IProjectionPrincipalGrain>(OperatorGuid);
        }
    }
}
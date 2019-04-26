using System.Collections.Generic;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class ProjectionPredicate : PredicateBase
    {
        public List<int> ProjectionAttrs;
        public ProjectionPredicate(List<int> projectionAttrs,int batchingLimit=1000):base(batchingLimit)
        {
            this.ProjectionAttrs=projectionAttrs;
        }
    }
}
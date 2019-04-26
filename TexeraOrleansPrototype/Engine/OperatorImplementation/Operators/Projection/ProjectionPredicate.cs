using System.Collections.Generic;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class ProjectionPredicate : PredicateBase
    {
        public List<int> ProjectionIndexs;
        public ProjectionPredicate(List<int> projectionIndexs,int batchingLimit=1000):base(batchingLimit)
        {
            this.ProjectionIndexs=projectionIndexs;
        }
    }
}
using System.Collections.Generic;
using Engine.Common;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanOperator : Operator
    {
        public ScanOperator(ScanPredicate predicate, int numberOfGrains) : base(predicate, numberOfGrains)
        {
        }
    }
}
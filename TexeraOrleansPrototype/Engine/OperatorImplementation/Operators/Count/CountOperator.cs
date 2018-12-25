using Engine.Common;
using Engine.OperatorImplementation.Common;
using System.Collections.Generic;

namespace Engine.OperatorImplementation.Operators
{
    public class CountOperator : Operator
    {
        public GrainIdentifier finalGrain;

        public CountOperator(CountPredicate predicate, int numberOfGrains) : base(predicate, numberOfGrains)
        {
            finalGrain = new GrainIdentifier(GetOperatorGuid(),"0");
        }

        public override List<GrainIdentifier> GetOutputGrainIDs()
        {
            return new List<GrainIdentifier>(){finalGrain};
        }
    }
}
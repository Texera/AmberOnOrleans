using Engine.Common;
using Engine.OperatorImplementation.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

        public override ReadOnlyCollection<GrainIdentifier> GetAllGrainsIDs()
        {
            List<GrainIdentifier> retList = new List<GrainIdentifier>(grainIDs);
            retList.Add(finalGrain);
            return retList.AsReadOnly();
        }
    }
}
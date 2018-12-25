using System;
using System.Collections.Generic;
using Engine.Common;

namespace Engine.OperatorImplementation.Common
{
    public abstract class Operator : IOperator
    {
        public int NumberOfGrains {get; set;}
        protected List<GrainIdentifier> grainIDs;
        private readonly Guid operatorGuid;
        public PredicateBase Predicate {get; set;}
        public Operator NextOperator {get; set;}

        public virtual void setNextOperator(Operator nextOperator)
        {
            this.NextOperator = nextOperator;
        }

        public Operator(PredicateBase predicate, int numberOfGrains)
        {
            this.operatorGuid = Guid.NewGuid();
            this.Predicate = predicate;
            this.NumberOfGrains = numberOfGrains;
            this.grainIDs = new List<GrainIdentifier>();
            for(int i=0; i<numberOfGrains; i++)
            {
                grainIDs.Add(new GrainIdentifier(GetOperatorGuid(), i.ToString()));
            }
        }

        public virtual List<GrainIdentifier> GetInputGrainIDs()
        {
            return grainIDs;
        }

        public virtual List<GrainIdentifier> GetOutputGrainIDs()
        {
            return grainIDs;
        }

        public Guid GetStreamGuid()
        {
            return operatorGuid;
        }

        public Guid GetOperatorGuid()
        {
            return operatorGuid;
        }
    }
}
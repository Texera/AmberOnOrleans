using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;
using Orleans;

namespace Engine.OperatorImplementation.Common
{
    public abstract class Operator
    {
        public readonly Guid OperatorGuid;
        public PredicateBase Predicate {get; set;}
        private HashSet<Operator> outOperators=new HashSet<Operator>();
        public IPrincipalGrain PrincipalGrain=null;
        public readonly bool IsStartOperator;
        public bool IsEndOperator {get{return outOperators.Count==0;}}

        public void AddOutOperator(Operator operatorToAdd)
        {
            outOperators.Add(operatorToAdd);
        }

        public HashSet<Operator> GetAllOutOperators()
        {
            return outOperators;
        }
        
        public virtual void SetUpPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<IPrincipalGrain>(OperatorGuid,"Principal");
        }
        public async Task LinkToDownstreamPrincipleGrains()
        {
            Trace.Assert(PrincipalGrain!=null, "PrincipalGrain should not be null when calling LinkToDownstreamPrincipleGrains()");
            foreach(Operator o in outOperators)
            {
                Trace.Assert(o.PrincipalGrain!=null,"PricipalGrain of the next Operator should not be null when calling LinkToDownstreamPrincipleGrains()");
                await PrincipalGrain.AddNextPrincipalGrain(o.PrincipalGrain);
            }
        }
        
        public Operator(PredicateBase predicate, bool isStartOperator=false)
        {
            this.IsStartOperator = isStartOperator;
            this.OperatorGuid = Guid.NewGuid();
            this.Predicate = predicate;
        }

        public bool Equals(Operator obj)
        {
            return obj.OperatorGuid==OperatorGuid;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) { return false; }
            return this.Equals(obj as Operator);
        }

        public override int GetHashCode()
        {
            return OperatorGuid.GetHashCode();
        }

        public Guid GetStreamGuid()
        {
            return OperatorGuid;
        }

        public async Task Link()
        {
            await PrincipalGrain.Link();
        }

        public async Task Pause()
        {
            await PrincipalGrain.Pause();
        }

        public async Task Resume()
        {
            await PrincipalGrain.Resume();
        }

    }
}
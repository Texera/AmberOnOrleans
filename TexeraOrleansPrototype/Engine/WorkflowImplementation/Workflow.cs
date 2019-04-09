using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using Engine.Controller;
using Orleans;
using System.Threading.Tasks;
using System;

namespace Engine.WorkflowImplementation
{
    public class Workflow
    {
        public HashSet<Operator> StartOperators = new HashSet<Operator>();
        public HashSet<Operator> AllOperators;
        public HashSet<Operator> EndOperators=new HashSet<Operator>();
        public readonly Guid WorkflowID;
        private IControllerGrain workflowControllerGrain=null;

        public Workflow(Guid workflowID)
        {
            this.WorkflowID=workflowID;
            
        }

        public void InitializeOperatorSet(HashSet<Operator> allOperators)
        {
            this.AllOperators=allOperators;
            foreach(Operator o in allOperators)
            {
                if(o.GetAllOutOperators().Count==0)
                    EndOperators.Add(o);
                if(o.IsStartOperator)
                    StartOperators.Add(o);
            }
        }

        public async Task Init(IGrainFactory factory)
        {
            workflowControllerGrain=factory.GetGrain<IControllerGrain>(WorkflowID);
            foreach(Operator o in AllOperators)
            {
                o.SetPrincipalGrain(factory);
            }
            await workflowControllerGrain.Init(workflowControllerGrain,WorkflowID,AllOperators);
        }

        public async Task Pause()
        {
            foreach(Operator o in AllOperators)
            {
                await o.Pause();
            }
            return;
        }

        public async Task Resume()
        {
            foreach(Operator o in AllOperators)
            {
                await o.Resume();
            }
        }


        public Guid GetStreamGuid()
        {
            return WorkflowID;
        }
    }
}
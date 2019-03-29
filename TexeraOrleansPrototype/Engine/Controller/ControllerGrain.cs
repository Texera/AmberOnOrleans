using Orleans;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using Engine.OperatorImplementation.Operators;
using Engine.OperatorImplementation.Common;
using Engine.WorkflowImplementation;

namespace Engine.Controller
{
    public class ControllerGrain : Grain, IControllerGrain
    {
        public Guid WorkflowID;
        private IControllerGrain self;
        public async Task Init(IControllerGrain self,Guid workflowID, HashSet<Operator> graph)
        {
            this.self=self;
            WorkflowID=workflowID;
            foreach(Operator o in graph)
            {
                o.SetUpPrincipalGrain(this.GrainFactory);
                await o.PrincipalGrain.Init(self,workflowID,o);
            }
            foreach(Operator o in graph)
            {
                await o.LinkPrincipleGrain();
            }
            foreach(Operator o in graph)
            {
                await o.Link();
            }
        }
    }
}
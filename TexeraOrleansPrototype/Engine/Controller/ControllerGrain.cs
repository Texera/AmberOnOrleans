using Orleans;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using Engine.OperatorImplementation.Operators;
using Engine.OperatorImplementation.Common;
using Engine.WorkflowImplementation;
using Orleans.Runtime;

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
                RequestContext.Set("grainIndex",0);
                await o.PrincipalGrain.Init(self,workflowID,o);
            }
            foreach(Operator o in graph)
            {
                await o.LinkPrincipleGrain();
            }
            foreach(Operator o in graph)
            {
                await o.LinkWorkerGrains();
            }
        }


        public async Task Pause(HashSet<Operator> graph)
        {
            List<Task> taskList=new List<Task>();
            foreach(Operator o in graph)
            {
                taskList.Add(o.Pause());
            }
            await Task.WhenAll(taskList);
        }
    }
}
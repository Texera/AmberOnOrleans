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
        public async Task Init(Workflow workflow)
        {
            foreach(Operator o in workflow.WorkflowGraph)
            {
                await o.Init();
            }
            foreach(Operator o in workflow.WorkflowGraph)
            {
                await o.Link();
            }
        }
    }
}
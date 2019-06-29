using Orleans;
using System.Threading.Tasks;
using Engine.WorkflowImplementation;
using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using System;

namespace Engine.Controller
{
    public interface IControllerGrain : IGrainWithGuidKey
    {
        Task Init(IControllerGrain self,Guid workflowID,HashSet<Operator> graph);
        Task Pause(HashSet<Operator> graph,int target);
        Task OnTaskDidPaused();
    }
}
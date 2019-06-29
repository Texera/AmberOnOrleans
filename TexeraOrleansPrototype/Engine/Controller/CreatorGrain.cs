using Orleans;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using Engine.OperatorImplementation.Operators;
using Engine.OperatorImplementation.Common;
using Engine.WorkflowImplementation;
using Orleans.Runtime;
using TexeraUtilities;

namespace Engine.Controller
{
    [WorkerGrainPlacement]
    public class CreatorGrain : Grain, ICreatorGrain
    {
        private IControllerGrain controller=null;
        public async Task Init(IControllerGrain self,Guid workflowID, HashSet<Operator> graph)
        {
            controller=GrainFactory.GetGrain<IControllerGrain>(new Guid());
            RequestContext.Set("targetSilo",Constants.ClientIPAddress);
            await controller.Init(controller,workflowID,graph);
        }


        public Task Pause(HashSet<Operator> graph)
        {
            controller.Pause(graph);
            return Task.CompletedTask;
        }


    }
}
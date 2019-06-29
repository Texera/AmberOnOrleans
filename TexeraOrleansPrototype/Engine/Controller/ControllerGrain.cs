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
    public class ControllerGrain : Grain, IControllerGrain
    {
        public Guid WorkflowID;
        private IControllerGrain self;
        private int currentPausedPrincipals=0;
        private int targetPausedPrincipals=0;
        private DateTime pauseStart;
        public async Task Init(IControllerGrain self,Guid workflowID, HashSet<Operator> graph)
        {
            this.self=self;
            targetPausedPrincipals=graph.Count;
            WorkflowID=workflowID;
            foreach(Operator o in graph)
            {
                RequestContext.Set("targetSilo",Constants.ClientIPAddress);
                await o.PrincipalGrain.Init(GrainFactory.GetGrain<IControllerGrain>(WorkflowID),workflowID,o);
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


        public Task Pause(HashSet<Operator> graph)
        {
            currentPausedPrincipals=0;
            pauseStart=DateTime.UtcNow;
            foreach(Operator o in graph)
            {
                o.Pause();
            }
            return Task.CompletedTask;
        }

        public Task Dummy()
        {
            Console.WriteLine("dummy received!");
            return Task.CompletedTask;
        }

        public Task OnTaskDidPaused()
        {   
            currentPausedPrincipals++;
            //Console.WriteLine(currentPausedPrincipals+"  "+targetPausedPrincipals);
            if(currentPausedPrincipals==targetPausedPrincipals)
            {
                TimeSpan duration=DateTime.UtcNow-pauseStart;
                Console.WriteLine("Workflow Paused in "+duration);
            }
            return Task.CompletedTask;
        }
    }
}
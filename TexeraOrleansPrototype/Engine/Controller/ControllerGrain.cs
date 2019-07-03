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
        private int currentRepliedPrincipals=0;
        private int targetRepliedPrincipals=0;
        private DateTime actionStart;
        private bool performingAction=false;
        public async Task Init(IControllerGrain self,Guid workflowID, HashSet<Operator> graph)
        {
            this.self=self;
            targetRepliedPrincipals=graph.Count;
            WorkflowID=workflowID;
            foreach(Operator o in graph)
            {
                RequestContext.Set("targetSilo",Constants.ClientIPAddress);
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


        public Task Pause(HashSet<Operator> graph)
        {
            if(performingAction)
            {
                Console.WriteLine("one action is performing, please wait...");
                return Task.CompletedTask;
            }
            performingAction=true;
            currentRepliedPrincipals=0;
            actionStart=DateTime.UtcNow;
            foreach(Operator o in graph)
            {
                o.Pause();
            }
            return Task.CompletedTask;
        }

        public Task OnTaskDidPaused()
        {   
            currentRepliedPrincipals++;
            //Console.WriteLine(currentPausedPrincipals+"  "+targetPausedPrincipals);
            if(currentRepliedPrincipals==targetRepliedPrincipals)
            {
                TimeSpan duration=DateTime.UtcNow-actionStart;
                Console.WriteLine("Workflow Paused in "+duration);
                performingAction=false;
            }
            return Task.CompletedTask;
        }


        public async Task Resume(HashSet<Operator> graph)
        {
            if(performingAction)
            {
                Console.WriteLine("one action is performing, please wait...");
                return;
            }
            performingAction=true;
            currentRepliedPrincipals=0;
            actionStart=DateTime.UtcNow;
            foreach(Operator o in graph)
            {
                await o.Resume();
            }
            TimeSpan duration=DateTime.UtcNow-actionStart;
            Console.WriteLine("Workflow Resumed in "+duration);
            performingAction=false;
        }
    }
}
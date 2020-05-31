using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Engine.OperatorImplementation.Operators;
using Engine.OperatorImplementation.SendingSemantics;
using TexeraUtilities;
using System.Collections.ObjectModel;
using Engine.Controller;
using System.Linq;
using Orleans.Placement;
using Orleans.Runtime;
using Orleans.Streams;
using System.Threading;
using ConcurrentCollections;
using Engine.OperatorImplementation.FaultTolerance;
using Engine.DeploySemantics;
using Engine.Breakpoint.GlobalBreakpoint;
using Engine.LinkSemantics;
using Engine.Breakpoint.LocalBreakpoint;

namespace Engine.OperatorImplementation.Common
{
    [WorkerGrainPlacement]
    public class PrincipalGrain : Grain, IPrincipalGrain
    {
        private enum ControlMessageType
        {
            UserPause,
            UserResume,
        }
        private Queue<ControlMessageType> controlMessageQueue=new Queue<ControlMessageType>();

        int flags = 0;
        private const int waitingThreshold = 100;
        private List<WorkerLayer> grainLayers = new List<WorkerLayer>();
        private List<LinkStrategy> links = new List<LinkStrategy>();
        private WorkerLayer outputLayer {get{return grainLayers.Last();}}
        private WorkerLayer inputLayer {get{return grainLayers.First();}}
        private IControllerGrain controllerGrain;
        private Dictionary<string,GlobalBreakpointBase> savedBreakpoints = new Dictionary<string,GlobalBreakpointBase>();
        private HashSet<string> triggeredBreakpointIDs = new HashSet<string>();
        private Operator operatorCore;
        private IPrincipalGrain self = null;
        private Dictionary<IWorkerGrain,WorkerState> workerStates = new Dictionary<IWorkerGrain, WorkerState>();
        private bool stateTransitioning = false;
        private bool needResume = false;

        public override Task OnDeactivateAsync()
        {
            Console.WriteLine("Principal: "+Utils.GetReadableName(self)+" deactivated");
            controlMessageQueue = null;
            grainLayers = null;
            links = null;
            savedBreakpoints = null;
            triggeredBreakpointIDs = null;
            workerStates = null;
            return base.OnDeactivateAsync();
        }

        public virtual async Task Init(IControllerGrain controllerGrain, Operator op, List<Pair<Operator,WorkerLayer>> prev)
        {
            this.controllerGrain=controllerGrain;
            this.self = this.GrainReference.Cast<IPrincipalGrain>();
            this.operatorCore = op;
            var topology = this.operatorCore.GenerateTopology();
            grainLayers = topology.First;
            links = topology.Second;
            foreach(var layer in grainLayers)
            {
                await layer.Build(self,GrainFactory,prev);
                foreach(IWorkerGrain worker in layer.Layer.Values.SelectMany(x=>x))
                {
                    workerStates[worker] = WorkerState.UnInitialized;
                }
            }
            foreach(var link in links)
            {
                await link.Link();
            }
        }

        public Task<WorkerLayer> GetInputLayer()
        {
            return Task.FromResult(inputLayer);
        }

        public Task<WorkerLayer> GetOutputLayer()
        {
            return Task.FromResult(outputLayer);
        }
        public async Task Pause()
        {
            Console.WriteLine("Principal: "+Utils.GetReadableName(self)+" received pause control message");
            if(stateTransitioning)
            {
                controlMessageQueue.Enqueue(ControlMessageType.UserPause);
            }
            else
            {
                if(workerStates.Values.All(x => x == WorkerState.Completed))
                {
                    Console.WriteLine("Principal: "+Utils.GetReadableName(self)+" paused");
                    await controllerGrain.OnPrincipalPaused(self);
                }
                else
                {
                    stateTransitioning = true;
                    foreach(WorkerLayer layer in grainLayers)
                    {
                        List<Task> taskList=new List<Task>();
                        foreach(IWorkerGrain worker in layer.Layer.Values.SelectMany(x=>x))
                        {
                            if(workerStates[worker]!=WorkerState.Completed)
                            {
                                workerStates[worker] = WorkerState.Pausing;
                            }
                        }
                        foreach(IWorkerGrain grain in layer.Layer.Values.SelectMany(x=>x))
                        {
                            if(workerStates[grain]!=WorkerState.Completed)
                            {
                                taskList.Add(grain.Pause());
                            }
                        }
                        await Task.WhenAll(taskList);
                    }
                }
            }
        }

        public async Task Resume()
        {
            Console.WriteLine("Principal: "+Utils.GetReadableName(self)+" received resume control message");
            if(stateTransitioning)
            {
                controlMessageQueue.Enqueue(ControlMessageType.UserResume);
            }
            else
            {
                foreach(WorkerLayer layer in grainLayers)
                {
                    List<Task> taskList=new List<Task>();
                    foreach(IWorkerGrain grain in layer.Layer.Values.SelectMany(x=>x))
                    {
                        if(workerStates[grain]!=WorkerState.Completed)
                        {
                            taskList.Add(grain.Resume());
                        }
                    }
                    await Task.WhenAll(taskList);
                    foreach(IWorkerGrain worker in layer.Layer.Values.SelectMany(x=>x))
                    {
                        if(workerStates[worker]!=WorkerState.Completed)
                        {
                            workerStates[worker] = WorkerState.Running;
                        }
                    }
                }
                Console.WriteLine("Principal: "+Utils.GetReadableName(self)+" resumed");
                ProcessNextControlMessage();
            }
        }


        public void ProcessNextControlMessage()
        {
            if(controlMessageQueue.Count > 0)
            {
                var t = controlMessageQueue.Dequeue();
                switch(t)
                {
                    case ControlMessageType.UserPause:
                        self.Pause();
                        break;
                    case ControlMessageType.UserResume:
                        self.Resume();
                        break;
                }
            }
        }


        public virtual async Task Start()
        {
            Console.WriteLine("Principal: "+Utils.GetReadableName(self) + " starting");
            foreach(WorkerLayer layer in grainLayers)
            {
                List<Task> taskList=new List<Task>();
                foreach(IWorkerGrain grain in layer.Layer.Values.SelectMany(x=>x))
                {
                    taskList.Add(grain.Start());
                }
                await Task.WhenAll(taskList);
            }
            Console.WriteLine("Principal: "+Utils.GetReadableName(self) + " started");
        }

        public async Task OnWorkerDidPaused(IWorkerGrain sender)
        {
            if(workerStates[sender]!=WorkerState.Completed)
            {
                workerStates[sender] = WorkerState.Paused;
                if(workerStates.Values.Where(x => x != WorkerState.Completed).All(x => x==WorkerState.Paused))
                {
                    Console.WriteLine("Principal: "+Utils.GetReadableName(self)+" paused");
                    stateTransitioning = false;
                    controllerGrain.OnPrincipalPaused(self);
                    //query breakpoints
                    foreach(var id in triggeredBreakpointIDs)
                    {
                        var bp = savedBreakpoints[id];
                        await bp.Collect();
                        if(bp.isTriggered)
                        {
                            await controllerGrain.OnBreakpointTriggered(bp.Report());
                            needResume = false;
                        }
                        if(bp.IsRepartitionRequired)
                        {
                            operatorCore.AssignBreakpoint(grainLayers,workerStates,savedBreakpoints[id]);
                        }
                        else if(bp.IsCompleted)
                        {
                            await bp.Remove();
                        }
                    }
                    if(needResume)
                    {
                        needResume = false;
                        await Resume();
                    }
                    ProcessNextControlMessage();
                }
            }
            else
            {
                Console.WriteLine("Warning: "+Utils.GetReadableName(sender)+" tries to become Paused after Completed");
            }
        }

        public virtual async Task Deactivate()
        {
            foreach(WorkerLayer layer in grainLayers)
            {
                foreach(IWorkerGrain grain in layer.Layer.Values.SelectMany(x=>x))
                {
                    await grain.Deactivate();
                }
            }
            DeactivateOnIdle();
        }

        public Task OnWorkerFinished(IWorkerGrain sender)
        {
            workerStates[sender] = WorkerState.Completed;
            if(workerStates.Values.All(x => x==WorkerState.Completed))
            {
                Console.WriteLine("Principal: "+this.GetPrimaryKey()+" completed!");
                controllerGrain.OnPrincipalCompleted(self);
            }
            return Task.CompletedTask;
        }

        public Task SetBreakpoint(GlobalBreakpointBase breakpoint)
        {
            throw new NotImplementedException();
        }

        public Task OnWorkerLocalBreakpointTriggered(IWorkerGrain sender, List<LocalBreakpointBase> breakpoint)
        {
            if(!stateTransitioning)
            {
                needResume = true;
                self.Pause();
            }
            foreach(LocalBreakpointBase bp in breakpoint)
            {
                triggeredBreakpointIDs.Add(bp.id);
                savedBreakpoints[bp.id].Accept(sender,bp);
            }
            return Task.CompletedTask;
        }

        public Task OnWorkerReceivedAllBatches(IWorkerGrain sender)
        {
            flags++;
            if(flags == workerStates.Count)
            {
                controllerGrain.OnPrincipalReceivedAllBatches(self);
            }
            return Task.CompletedTask;
        }

        public Task StashOutput()
        {
            foreach(WorkerLayer layer in grainLayers)
            {
                foreach(IWorkerGrain grain in layer.Layer.Values.SelectMany(x=>x))
                {
                    grain.StashOutput();
                }
            }
            return Task.CompletedTask;
        }

        public async Task ReleaseOutput()
        {
            List<Task> taskList = new List<Task>();
            foreach(WorkerLayer layer in grainLayers)
            {
                foreach(IWorkerGrain grain in layer.Layer.Values.SelectMany(x=>x))
                {
                    taskList.Add(grain.ReleaseOutput());
                }
            }
            await Task.WhenAll(taskList);
        }
    }
}

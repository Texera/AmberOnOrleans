using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Engine.OperatorImplementation.Operators;
using TexeraUtilities;
using System.Collections.ObjectModel;
using Engine.Controller;
using System.Linq;

namespace Engine.OperatorImplementation.Common
{
    public class PrincipalGrain : Grain, IPrincipalGrain
    {
        public virtual int DefaultNumGrainsInOneLayer { get { return 2; } }
        private List<IPrincipalGrain> nextPrincipalGrains = new List<IPrincipalGrain>();
        private List<IPrincipalGrain> prevPrincipalGrains = new List<IPrincipalGrain>();
        protected bool isPaused = false;
        protected List<List<IWorkerGrain>> operatorGrains = new List<List<IWorkerGrain>>();
        protected List<IWorkerGrain> outputGrains {get{return operatorGrains.Last();}}
        protected List<IWorkerGrain> inputGrains {get{return operatorGrains.First();}}
        private IPrincipalGrain self=null;
        private Guid workflowID;
        private IControllerGrain controllerGrain;
        private ulong sequenceNumber=0;


        public virtual IWorkerGrain GetOperatorGrain(string extension)
        {
            throw new NotImplementedException();
        }

        public Task AddNextPrincipalGrain(IPrincipalGrain nextGrain)
        {
            nextPrincipalGrains.Add(nextGrain);
            return Task.CompletedTask;
        }

        public Task AddPrevPrincipalGrain(IPrincipalGrain prevGrain)
        {
            prevPrincipalGrains.Add(prevGrain);
            return Task.CompletedTask;
        }

        public async Task Init(IControllerGrain controllerGrain, Guid workflowID, Operator currentOperator)
        {
            this.controllerGrain=controllerGrain;
            this.workflowID=workflowID;
            this.self=currentOperator.PrincipalGrain;
            PredicateBase predicate=currentOperator.Predicate;
            await BuildWorkerTopology();
            PassExtraParametersByPredicate(ref predicate);
            foreach(List<IWorkerGrain> grainList in operatorGrains)
            {
                foreach(IWorkerGrain grain in grainList)
                {
                    await grain.Init(grain,predicate,self);
                }
            }
        }


        protected virtual void PassExtraParametersByPredicate(ref PredicateBase predicate)
        {
            
        }

        public virtual async Task BuildWorkerTopology()
        {
            operatorGrains=Enumerable.Range(0, 1).Select(x=>new List<IWorkerGrain>()).ToList();
            //one-layer init
            for(int i=0;i<DefaultNumGrainsInOneLayer;++i)
            {
                IWorkerGrain grain=GetOperatorGrain(i.ToString());
                operatorGrains[0].Add(grain);
            }
            // for multiple-layer init, do some linking inside...
        }

        public async Task Link()
        {
            int count=0;
            foreach(IPrincipalGrain principal in prevPrincipalGrains)
            {
                List<IWorkerGrain> prevOutputGrains=await principal.GetOutputGrains();
                count+=prevOutputGrains.Count;
            }
            if(count>0)
            {
                foreach(IWorkerGrain grain in inputGrains)
                {
                    await grain.SetTargetEndFlagCount(count);
                }
            }

            if(nextPrincipalGrains.Count!=0)
            {
                foreach(IPrincipalGrain principal in nextPrincipalGrains)
                {
                    List<IWorkerGrain> nextInputGrains=await principal.GetInputGrains();
                    await Link2Layers(principal.GetPrimaryKey(),outputGrains,nextInputGrains);
                }
            }
            else
            {
                //last operator, build stream
                var streamProvider = GetStreamProvider("SMSProvider");
                foreach(IWorkerGrain grain in outputGrains)
                    await grain.InitializeOutputStream(streamProvider.GetStream<Immutable<PayloadMessage>>(workflowID,"OutputStream"));
            }
        }

        protected async Task Link2Layers(Guid nextOperatorGuid, List<IWorkerGrain> currentLayer,List<IWorkerGrain> nextLayer)
        {
            for(int i=0;i<currentLayer.Count;++i)
            {
                await currentLayer[i].AddNextGrainList(nextOperatorGuid,nextLayer);
            }
        }

        public Task<List<IWorkerGrain>> GetInputGrains()
        {
            return Task.FromResult(inputGrains);
        }

        public Task<List<IWorkerGrain>> GetOutputGrains()
        {
            return Task.FromResult(outputGrains);
        }

        public string MakeIndentifier(IPrincipalGrain grain)
        {
            string extension;
            return grain.GetPrimaryKey(out extension).ToString()+extension;
        }

        public virtual async Task Pause()
        {
            isPaused = true;
            foreach(List<IWorkerGrain> grainList in operatorGrains)
            {
                foreach(IWorkerGrain grain in grainList)
                {
                    await grain.ProcessControlMessage(new Immutable<ControlMessage>(new ControlMessage(MakeIndentifier(this),sequenceNumber,ControlMessage.ControlMessageType.Pause)));
                }
            }
            sequenceNumber++;
             foreach(IPrincipalGrain next in nextPrincipalGrains)
            {
                await SendPauseToNextPrincipalGrain(next,0);
            }
        }

        private async Task SendPauseToNextPrincipalGrain(IPrincipalGrain nextGrain, int retryCount)
        {
            nextGrain.Pause().ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    SendPauseToNextPrincipalGrain(nextGrain,retryCount+1);
            });
        }

        public virtual async Task Resume()
        {
            isPaused = false;
            foreach(IPrincipalGrain next in nextPrincipalGrains)
            {
                await SendResumeToNextPrincipalGrain(next,0);
            }
            foreach(List<IWorkerGrain> grainList in operatorGrains)
            {
                foreach(IWorkerGrain grain in grainList)
                {
                 await grain.ProcessControlMessage(new Immutable<ControlMessage>(new ControlMessage(MakeIndentifier(this),sequenceNumber,ControlMessage.ControlMessageType.Resume)));
                }
            }
            sequenceNumber++;
        }

        private async Task SendResumeToNextPrincipalGrain(IPrincipalGrain nextGrain, int retryCount)
        {
            nextGrain.Resume().ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    SendResumeToNextPrincipalGrain(nextGrain,retryCount+1);
            });
        }

        public virtual async Task Start()
        {
            foreach(IWorkerGrain grain in inputGrains)
            {
                 await grain.ProcessControlMessage(new Immutable<ControlMessage>(new ControlMessage(MakeIndentifier(this),sequenceNumber,ControlMessage.ControlMessageType.Start)));
            }
            sequenceNumber++;
        }
    }
}

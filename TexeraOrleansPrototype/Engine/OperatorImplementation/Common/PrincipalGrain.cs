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

namespace Engine.OperatorImplementation.Common
{
    public class PrincipalGrain : Grain, IPrincipalGrain
    {
        public virtual int DefaultNumGrainsInOneLayer { get { return 6; } }
        private List<IPrincipalGrain> nextPrincipalGrains = new List<IPrincipalGrain>();
        private List<IPrincipalGrain> prevPrincipalGrains = new List<IPrincipalGrain>();
        protected bool isPaused = false;
        protected Guid operatorID;
        protected List<List<IWorkerGrain>> operatorGrains = new List<List<IWorkerGrain>>();
        protected List<IWorkerGrain> outputGrains {get{return operatorGrains.Last();}}
        protected List<IWorkerGrain> inputGrains {get{return operatorGrains.First();}}
        protected PredicateBase predicate;
        protected IPrincipalGrain self=null;
        private Guid workflowID;
        private IControllerGrain controllerGrain;
        private ulong sequenceNumber=0;
        private int currentPauseFlag=0;
        protected IAsyncObserver<Immutable<ControlMessage>> controlMessageStream;
        
        public virtual async Task<IWorkerGrain> GetOperatorGrain(string extension)
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
            this.operatorID=currentOperator.OperatorGuid;
            this.self=currentOperator.PrincipalGrain;
            this.predicate=currentOperator.Predicate;
            PassExtraParametersByPredicate(ref this.predicate);
            var provider=GetStreamProvider("SMSProvider");
            this.controlMessageStream=provider.GetStream<Immutable<ControlMessage>>(self.GetPrimaryKey(),"Ctrl");
            await BuildWorkerTopology();
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
                IWorkerGrain grain=await GetOperatorGrain(i.ToString());
                operatorGrains[0].Add(grain);
            }
            // for multiple-layer init, do some linking inside...
        }

        public async Task LinkWorkerGrains()
        {
            Dictionary<Guid,int> inputInfo=new Dictionary<Guid, int>();
            foreach(IPrincipalGrain prevPrincipal in prevPrincipalGrains)
            {
                List<IWorkerGrain> prevOutputGrains=await prevPrincipal.GetOutputGrains();
                inputInfo[prevPrincipal.GetPrimaryKey()]=prevOutputGrains.Count;
            }
            if(inputInfo.Count>0)
            {
                foreach(IWorkerGrain grain in inputGrains)
                {
                    await grain.SetInputInformation(inputInfo);
                }
            }

            if(nextPrincipalGrains.Count!=0)
            {
                foreach(IPrincipalGrain nextPrincipal in nextPrincipalGrains)
                {
                    ISendStrategy strategy = await nextPrincipal.GetInputSendStrategy(self);
                    for(int i=0;i<outputGrains.Count;++i)
                    {
                        await outputGrains[i].SetSendStrategy(operatorID,strategy);
                    }
                }
            }
            else
            {
                //last operator, build stream
                var streamProvider = GetStreamProvider("SMSProvider");
                var stream = streamProvider.GetStream<Immutable<PayloadMessage>>(workflowID,"OutputStream");
                ISendStrategy strategy=new SendToStream(stream);
                foreach(IWorkerGrain grain in outputGrains)
                {
                    await grain.SetSendStrategy(workflowID,strategy);
                }
            }
        }

        // protected async Task Link2Layers(Guid nextOperatorGuid, List<IWorkerGrain> currentLayer,List<IWorkerGrain> nextLayer)
        // {
        //     for(int i=0;i<currentLayer.Count;++i)
        //     {
        //         await currentLayer[i].AddNextGrainList(nextOperatorGuid,nextLayer);
        //     }
        // }

        public Task<List<IWorkerGrain>> GetInputGrains()
        {
            return Task.FromResult(inputGrains);
        }

        public Task<List<IWorkerGrain>> GetOutputGrains()
        {
            return Task.FromResult(outputGrains);
        }

        private string ReturnGrainIndentifierString(IPrincipalGrain grain)
        {
            string extension;
            return grain.GetPrimaryKey(out extension).ToString()+extension;
        }

        public virtual async Task Pause()
        {
            currentPauseFlag++;
            if(currentPauseFlag>=prevPrincipalGrains.Count || isPaused)
            {
                currentPauseFlag=0;
                if(isPaused)
                {
                    return;
                }
                isPaused = true;
                foreach(List<IWorkerGrain> grainList in operatorGrains)
                {
                    List<Task> taskList=new List<Task>();
                    foreach(IWorkerGrain grain in grainList)
                    {
                        taskList.Add(grain.OnNextAsync(new Immutable<ControlMessage>(new ControlMessage(self,sequenceNumber,ControlMessage.ControlMessageType.Pause))));
                    }
                    await Task.WhenAll(taskList);
                }
                sequenceNumber++;
                foreach(IPrincipalGrain next in nextPrincipalGrains)
                {
                    await SendPauseToNextPrincipalGrain(next,0);
                }
            }
        }

        private async Task SendPauseToNextPrincipalGrain(IPrincipalGrain nextGrain, int retryCount)
        {
            await nextGrain.Pause().ContinueWith(async (t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    await SendPauseToNextPrincipalGrain(nextGrain,retryCount+1);
            });
        }

        public virtual async Task Resume()
        {
            if(!isPaused)
            {
                return;
            }
            foreach(IPrincipalGrain next in nextPrincipalGrains)
            {
                await SendResumeToNextPrincipalGrain(next,0);
            }
            isPaused = false;
            await controlMessageStream.OnNextAsync(new Immutable<ControlMessage>(new ControlMessage(self,sequenceNumber,ControlMessage.ControlMessageType.Resume)));
            sequenceNumber++;
        }

        private async Task SendResumeToNextPrincipalGrain(IPrincipalGrain nextGrain, int retryCount)
        {
            await nextGrain.Resume().ContinueWith(async (t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    await SendResumeToNextPrincipalGrain(nextGrain,retryCount+1);
            });
        }

        public virtual async Task Start()
        {
            await controlMessageStream.OnNextAsync(new Immutable<ControlMessage>(new ControlMessage(self,sequenceNumber,ControlMessage.ControlMessageType.Start)));
            sequenceNumber++;
        }

        public virtual Task<ISendStrategy> GetInputSendStrategy(IGrain requester)
        {
            return Task.FromResult(new RoundRobin(inputGrains,predicate.BatchingLimit) as ISendStrategy);
        }


        public virtual async Task Deactivate()
        {
            List<Task> taskList=new List<Task>();
            await controlMessageStream.OnNextAsync(new Immutable<ControlMessage>(new ControlMessage(self,sequenceNumber,ControlMessage.ControlMessageType.Deactivate)));
            sequenceNumber++;
            DeactivateOnIdle();
        }
    }
}

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

namespace Engine.OperatorImplementation.Common
{
    public class PrincipalGrain : Grain, IPrincipalGrain
    {
        protected virtual int DefaultNumGrainsInOneLayer { get { return 10; } }
        private List<IPrincipalGrain> NextPrincipalGrains = new List<IPrincipalGrain>();
        protected bool isPaused = false;
        protected List<INormalGrain> operatorGrains = new List<INormalGrain>();
        protected List<INormalGrain> inputGrains = new List<INormalGrain>();
        protected List<INormalGrain> outputGrains = new List<INormalGrain>();
        private PredicateBase predicate = null;


        public virtual INormalGrain GetOperatorGrain(string extension)
        {
            throw new NotImplementedException();
        }

        public Task AddNextPrincipalGrain(IPrincipalGrain nextGrain)
        {
            NextPrincipalGrains.Add(nextGrain);
            return Task.CompletedTask;
        }

        public Task SetPredicate(PredicateBase predicate)
        {
            this.predicate=predicate;
            return Task.CompletedTask;
        }

        public virtual async Task Init(PredicateBase predicate)
        {
            //one-layer init
            for(int i=0;i<DefaultNumGrainsInOneLayer;++i)
            {
                INormalGrain grain=GetOperatorGrain(i.ToString());
                await grain.Init(predicate);
                operatorGrains.Add(grain);
                inputGrains.Add(grain);
                outputGrains.Add(grain);
            }
            // for multiple-layer init, do some linking inside...
        }

        public async Task Link()
        {
            if(NextPrincipalGrains.Count!=0)
            {
                foreach(IPrincipalGrain principal in NextPrincipalGrains)
                {
                    List<INormalGrain> nextInputGrains=await principal.GetInputGrains();
                    await Link2Layers(outputGrains,nextInputGrains);
                }
            }
            else
            {
                //last operator, build stream
                var streamProvider = GetStreamProvider("SMSProvider");
                foreach(INormalGrain grain in outputGrains)
                    await grain.AddNextStream(streamProvider.GetStream<Immutable<TexeraMessage>>(this.GetPrimaryKey(),"OutputStream"));
            }
        }

        protected async Task Link2Layers(List<INormalGrain> currentLayer,List<INormalGrain> nextLayer)
        {
            for(int i=0;i<currentLayer.Count;++i)
            {
                await currentLayer[i].Link(await currentLayer[i].GetNextGrains(nextLayer));
            }
        }

        public Task<List<INormalGrain>> GetInputGrains()
        {
            return Task.FromResult(inputGrains);
        }

        public virtual async Task Pause()
        {
            isPaused = true;
            foreach(INormalGrain grain in operatorGrains)
            {
                await grain.Pause();
            }
             foreach(IPrincipalGrain next in NextPrincipalGrains)
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
            foreach(IPrincipalGrain next in NextPrincipalGrains)
            {
                await SendResumeToNextPrincipalGrain(next,0);
            }
            foreach(INormalGrain grain in operatorGrains)
            {
                await grain.Resume();
            }
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
            foreach(INormalGrain op in operatorGrains)
            {
                await op.Start();
            }
        }
    }
}

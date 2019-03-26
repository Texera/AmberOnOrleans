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
        protected List<INormalGrain> outputGrains=new List<INormalGrain>();
        private PredicateBase predicate = null;
        private INormalGrain GetOperatorGrain(string extension)
        {
            INormalGrain currGrain = null;
            Guid primary = this.GetPrimaryKey();
            switch(predicate)
            {
                case ScanPredicate o:
                    currGrain = this.GrainFactory.GetGrain<IScanOperatorGrain>(primary, extension);
                    break;
                case FilterPredicate o:
                    currGrain = this.GrainFactory.GetGrain<IFilterOperatorGrain>(primary, extension);
                    break;
                case KeywordPredicate o:
                    currGrain = this.GrainFactory.GetGrain<IKeywordSearchOperatorGrain>(primary, extension);
                    break;
                default:
                    //others
                    throw new NotImplementedException();
            }

            return currGrain;
        }

        public Task AddNextPrincipalGrain(IPrincipalGrain nextGrain)
        {
            NextPrincipalGrains.Add(nextGrain);
            return Task.CompletedTask;
        }

        public virtual async Task Init(PredicateBase pred)
        {
            predicate=pred;
            //one-layer init
            for(int i=0;i<DefaultNumGrainsInOneLayer;++i)
            {
                INormalGrain grain=GetOperatorGrain(i.ToString());
                await grain.Init(pred);
                inputGrains.Add(grain);
                operatorGrains.Add(grain);
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
                if(await currentLayer[i].NeedCustomSending())
                {
                    //custom(e.g. distributed equi-hash join)
                    await currentLayer[i].AddNextGrain(nextLayer);
                }
                else
                {
                    if(currentLayer.Count==nextLayer.Count)
                    {
                        //one-to-one
                        await currentLayer[i].AddNextGrain(nextLayer[i]);
                    }
                    else if(currentLayer.Count>nextLayer.Count)
                    {
                        //many-to-one
                        await currentLayer[i].AddNextGrain(nextLayer[i%nextLayer.Count]);
                    }
                    else
                    {
                        //one-to-many (round-robin)
                        for(int j=i;j<nextLayer.Count;j+=currentLayer.Count)
                        {
                            await currentLayer[i].AddNextGrain(nextLayer[j]);
                        }
                    }
                }
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

    }
}

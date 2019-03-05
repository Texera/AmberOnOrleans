using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Common
{
    public class PrincipalGrain : Grain, IPrincipalGrain
    {
        public IPrincipalGrain nextPrincipalGrain = null;
        public bool IsLastPrincipalGrain = false;
        protected bool pause = false;
        protected List<INormalGrain> operatorGrains = new List<INormalGrain>();

        public virtual async Task<IPrincipalGrain> GetNextPrincipalGrain()
        {
            return nextPrincipalGrain;
        }

        public virtual Task SetNextPrincipalGrain(IPrincipalGrain nextPrincipalGrain)
        {
            this.nextPrincipalGrain = nextPrincipalGrain;
            return Task.CompletedTask;
        }

        public Task SetIsLastPrincipalGrain(bool isLastPrincipalGrain)
        {
            this.IsLastPrincipalGrain = isLastPrincipalGrain;
            return Task.CompletedTask;
        }

        public async Task<bool> GetIsLastPrincipalGrain()
        {
            return IsLastPrincipalGrain;
        }

        public virtual Task Init()
        {
            return Task.CompletedTask;
        }

        public Task SetOperatorGrains(List<INormalGrain> operatorGrains)
        {
            this.operatorGrains = operatorGrains;
            return Task.CompletedTask;
        }

        public virtual async Task PauseGrain()
        {
            pause = true;
            foreach(INormalGrain grain in operatorGrains)
            {
                await grain.PauseGrain();
            }
            
            if(nextPrincipalGrain != null)
            {
                await SendPauseToNextPrincipalGrain(nextPrincipalGrain,0);
            }
        }

        private async Task SendPauseToNextPrincipalGrain(IPrincipalGrain nextGrain, int retryCount)
        {
            nextGrain.PauseGrain().ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    SendPauseToNextPrincipalGrain(nextGrain,retryCount+1);
            });
        }

        public virtual async Task ResumeGrain()
        {
            pause = false;
            foreach(INormalGrain grain in operatorGrains)
            {
                await grain.ResumeGrain();
            }

            if(nextPrincipalGrain != null)
            {
                await SendResumeToNextPrincipalGrain(nextPrincipalGrain,0);
            }
        }

        private async Task SendResumeToNextPrincipalGrain(IPrincipalGrain nextGrain, int retryCount)
        {
            nextGrain.ResumeGrain().ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    SendResumeToNextPrincipalGrain(nextGrain,retryCount+1);
            });
        }
    }
}

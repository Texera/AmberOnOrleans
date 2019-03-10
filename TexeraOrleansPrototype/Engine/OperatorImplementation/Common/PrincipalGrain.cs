using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Engine.Common;
using Engine.OperatorImplementation.Operators;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Common
{
    public class PrincipalGrain : Grain, IPrincipalGrain
    {
        public IPrincipalGrain nextPrincipalGrain = null;
        public bool IsLastPrincipalGrain = false;
        protected bool pause = false;
        protected List<INormalGrain> operatorGrains = new List<INormalGrain>();
        protected Operator holdingOperator = null;

        public Task SetOperator(Operator op)
        {
            this.holdingOperator = op;
            return Task.CompletedTask;
        }

        public Task<Operator> GetOperator()
        {
            return Task.FromResult(holdingOperator);
        }

        public virtual async Task<IPrincipalGrain> GetNextPrincipalGrain()
        {
            return nextPrincipalGrain;
        }

        /**
        Does two things:
        1. Connects principal grain to operator grains
        2. Connects operators grains to next operators grains
         */
        public async Task SetUpAndConnectOperatorGrains()
        {
            Trace.Assert(holdingOperator!=null, "Holding operator not set in principal grain before calling SetupAndConnectOperatorGrains");
            await ConnectPrincipalGrainToOperatorGrains();

            if(IsLastPrincipalGrain)
            {
                return;
            }

            Trace.Assert(nextPrincipalGrain!=null, "NextPrincipalGrain is null but IsLastPrincipalGrain property is not set to null");
            Operator nextOperator = await nextPrincipalGrain.GetOperator();

            await ConnectOperatorGrainsToNextOperatorGrains(nextOperator);
        }

        private async Task ConnectPrincipalGrainToOperatorGrains()
        {
            List<INormalGrain> grainsInOperator = new List<INormalGrain>();

            // connect to member grains
            /***
            * TODO: What will happen if there are more internal grains other than input and output.
            * These grains will also have to be connected to principal grain.
                */
            List<GrainIdentifier> operatorOutputGrainIDs = holdingOperator.GetOutputGrainIDs();
            foreach(GrainIdentifier grainID in operatorOutputGrainIDs)
            {
                INormalGrain currGrain = GetOperatorGrainByType(holdingOperator, grainID, true);
                await currGrain.SetPredicate(holdingOperator.Predicate);
                await currGrain.Init();
                grainsInOperator.Add(currGrain);
            }

            List<GrainIdentifier> operatorInputGrainIDs = holdingOperator.GetInputGrainIDs();
            foreach(GrainIdentifier grainID in operatorInputGrainIDs)
            {
                INormalGrain currGrain = GetOperatorGrainByType(holdingOperator, grainID, false);
                grainsInOperator.Add(currGrain);
            }

            operatorGrains = grainsInOperator;
        }

        private async Task ConnectOperatorGrainsToNextOperatorGrains(Operator nextOperator)
        {
            List<GrainIdentifier> currOpOutputGrainIDs = holdingOperator.GetOutputGrainIDs();
            
            for(int i=0; i<currOpOutputGrainIDs.Count; i++)
            {
                INormalGrain currGrain = GetOperatorGrainByType(holdingOperator, currOpOutputGrainIDs[i], true);
                
                if(nextOperator != null)
                {
                    List<GrainIdentifier> nextOpInputGrainIDs = nextOperator.GetInputGrainIDs();
                    GrainIdentifier nextGrainID = nextOpInputGrainIDs[i%nextOpInputGrainIDs.Count];
                    switch(nextOperator)
                    {
                        case ScanOperator o:
                            IScanOperatorGrain scanGrain = this.GrainFactory.GetGrain<IScanOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                            // await currGrain.SetNextGrain(scanGrain);
                            break;
                        case FilterOperator o:
                            IFilterOperatorGrain filterGrain = this.GrainFactory.GetGrain<IFilterOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                            await currGrain.SetNextGrain(filterGrain);
                            break;
                        case KeywordOperator o:
                            IKeywordSearchOperatorGrain keywordGrain = this.GrainFactory.GetGrain<IKeywordSearchOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                            await currGrain.SetNextGrain(keywordGrain);
                            break;
                        case CountOperator o:
                            ICountOperatorGrain countGrain = this.GrainFactory.GetGrain<ICountOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                            await currGrain.SetNextGrain(countGrain);

                            //TODO: There is a bug below. It only connects those intermediary count grains to final grains which are used as an output by the currentGrain.
                            // Ideally this linking of grains within an operator should be done somewhere else.
                            ICountFinalOperatorGrain countFinalGrain = this.GrainFactory.GetGrain<ICountFinalOperatorGrain>(o.finalGrain.PrimaryKey, o.finalGrain.ExtensionKey);
                            await countGrain.SetNextGrain(countFinalGrain);
                            break;
                    }
                }
                
            }
        }

        private INormalGrain GetOperatorGrainByType(Operator currOperator, GrainIdentifier gid, bool outputGrain)
        {
            INormalGrain currGrain = null;

            switch(currOperator)
            {
                case ScanOperator o:
                    currGrain = this.GrainFactory.GetGrain<IScanOperatorGrain>(gid.PrimaryKey, gid.ExtensionKey);
                    break;
                case FilterOperator o:
                    currGrain = this.GrainFactory.GetGrain<IFilterOperatorGrain>(gid.PrimaryKey, gid.ExtensionKey);
                    break;
                case KeywordOperator o:
                    currGrain = this.GrainFactory.GetGrain<IKeywordSearchOperatorGrain>(gid.PrimaryKey, gid.ExtensionKey);
                    break;
                case CountOperator o:
                    if(outputGrain)
                    {
                        currGrain = this.GrainFactory.GetGrain<ICountFinalOperatorGrain>(gid.PrimaryKey, gid.ExtensionKey);
                    }
                    else
                    {
                        currGrain = this.GrainFactory.GetGrain<ICountOperatorGrain>(gid.PrimaryKey, gid.ExtensionKey);
                    }
                    break;
            }

            return currGrain;
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

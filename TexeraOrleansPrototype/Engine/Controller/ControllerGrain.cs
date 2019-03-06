using Orleans;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Engine.Common;
using Engine.OperatorImplementation.Operators;
using Engine.OperatorImplementation.Common;
using Engine.WorkflowImplementation;

namespace Engine.Controller
{
    public class ControllerGrain : Grain, IControllerGrain
    {
        public async Task SetUpAndConnectGrains(Workflow workflow)
        {
            await SetupAndConnectPrincipalGrains(workflow);
        }

        public async Task SetupAndConnectPrincipalGrains(Workflow workflow)
        {
            Operator currentOperator;
            Operator nextOperator;

            currentOperator = workflow.StartOperator;
            while(currentOperator != null)
            {   
                IPrincipalGrain pGrain = GetPrincipalGrainByType(currentOperator);
                await pGrain.Init();
                await pGrain.SetOperator(currentOperator);

                // set next principal grain
                nextOperator = currentOperator.NextOperator;
                if(nextOperator != null)
                {
                    IPrincipalGrain nextPrincipalGrain = this.GrainFactory.GetGrain<IPrincipalGrain>(nextOperator.GetPrincipalGrainID().PrimaryKey, nextOperator.GetPrincipalGrainID().ExtensionKey);
                    await pGrain.SetNextPrincipalGrain(nextPrincipalGrain);
                }

                if(nextOperator == null)
                {
                    await pGrain.SetIsLastPrincipalGrain(true);
                }
                currentOperator = nextOperator;
            }

            currentOperator = workflow.StartOperator;
            while(currentOperator != null)
            {
                IPrincipalGrain pGrain = GetPrincipalGrainByType(currentOperator);
                await pGrain.SetUpAndConnectOperatorGrains();

                currentOperator = currentOperator.NextOperator;
            }
        }

        public async Task CreateStreamFromLastOperator(Workflow workflow)
        {
            Operator currentOperator;
            Operator nextOperator;

            currentOperator = workflow.StartOperator;
            nextOperator = currentOperator.NextOperator;

            while(nextOperator != null)
            {
                currentOperator = nextOperator;
                nextOperator = currentOperator.NextOperator;
            }

            List<GrainIdentifier> currOpOutputGrainIDs = currentOperator.GetOutputGrainIDs();
            for(int i=0; i<currOpOutputGrainIDs.Count; i++)
            {
                INormalGrain currGrain = GetOperatorGrainByType(currentOperator, currOpOutputGrainIDs[i], true);
                await currGrain.SetIsLastOperatorGrain(true);
            }

        }

        public IPrincipalGrain GetPrincipalGrainByType(Operator currOperator)
        {
            IPrincipalGrain pGrain = null;
            switch(currOperator)
            {
                case ScanOperator o:
                    pGrain = this.GrainFactory.GetGrain<IScanPrincipalGrain>(currOperator.GetPrincipalGrainID().PrimaryKey, currOperator.GetPrincipalGrainID().ExtensionKey);
                    break;
                default:
                    pGrain = this.GrainFactory.GetGrain<IPrincipalGrain>(currOperator.GetPrincipalGrainID().PrimaryKey, currOperator.GetPrincipalGrainID().ExtensionKey);
                    break;
            }

            return pGrain;
        }

        public INormalGrain GetOperatorGrainByType(Operator currOperator, GrainIdentifier gid, bool outputGrain)
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
    }
}
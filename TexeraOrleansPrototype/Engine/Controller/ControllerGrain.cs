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
            await SetupAndConnectOperatorGrains(workflow);
            await SetupAndConnectPrincipalGrains(workflow);
        }

        public async Task SetupAndConnectPrincipalGrains(Workflow workflow)
        {
            Operator currentOperator;
            Operator nextOperator;

            currentOperator = workflow.StartOperator;
            while(currentOperator != null)
            {   
                IPrincipalGrain pGrain = this.GrainFactory.GetGrain<IPrincipalGrain>(currentOperator.GetPrincipalGrainID().PrimaryKey, currentOperator.GetPrincipalGrainID().ExtensionKey);
                await pGrain.Init();
                List<INormalGrain> grainsInOperator = new List<INormalGrain>();

                // connect to member grains
                /***
                * TODO: What will happen if there are more internal grains other than input and output.
                * These grains will also have to be connected to principal grain.
                 */
                List<GrainIdentifier> operatorOutputGrainIDs = currentOperator.GetOutputGrainIDs();
                foreach(GrainIdentifier grainID in operatorOutputGrainIDs)
                {
                    INormalGrain currGrain = GetOperatorGrainByType(currentOperator, grainID, true);
                    grainsInOperator.Add(currGrain);
                }

                List<GrainIdentifier> operatorInputGrainIDs = currentOperator.GetInputGrainIDs();
                foreach(GrainIdentifier grainID in operatorInputGrainIDs)
                {
                    INormalGrain currGrain = GetOperatorGrainByType(currentOperator, grainID, false);
                    grainsInOperator.Add(currGrain);
                }

                await pGrain.SetOperatorGrains(grainsInOperator);

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
        }

        public async Task SetupAndConnectOperatorGrains(Workflow workflow)
        {
            Operator currentOperator;
            Operator nextOperator;

            currentOperator = workflow.StartOperator;
            while(currentOperator != null)
            {
                nextOperator = currentOperator.NextOperator;
                List<GrainIdentifier> currOpOutputGrainIDs = currentOperator.GetOutputGrainIDs();
            
                for(int i=0; i<currOpOutputGrainIDs.Count; i++)
                {
                    INormalGrain currGrain = GetOperatorGrainByType(currentOperator, currOpOutputGrainIDs[i], true);
                    await currGrain.SetPredicate(currentOperator.Predicate);
                    await currGrain.Init();
                    
                    if(nextOperator != null)
                    {
                        List<GrainIdentifier> nextOpInputGrainIDs = nextOperator.GetInputGrainIDs();
                        GrainIdentifier nextGrainID = nextOpInputGrainIDs[i%nextOpInputGrainIDs.Count];
                        switch(nextOperator)
                        {
                            case ScanOperator o:
                                IScanOperatorGrain scanGrain = this.GrainFactory.GetGrain<IScanOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                                await scanGrain.Init();
                                // await currGrain.SetNextGrain(scanGrain);
                                break;
                            case FilterOperator o:
                                IFilterOperatorGrain filterGrain = this.GrainFactory.GetGrain<IFilterOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                                await filterGrain.Init();
                                await currGrain.SetNextGrain(filterGrain);
                                break;
                            case KeywordOperator o:
                                IKeywordSearchOperatorGrain keywordGrain = this.GrainFactory.GetGrain<IKeywordSearchOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                                await keywordGrain.Init();
                                await currGrain.SetNextGrain(keywordGrain);
                                break;
                            case CountOperator o:
                                ICountOperatorGrain countGrain = this.GrainFactory.GetGrain<ICountOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                                await countGrain.Init();
                                await currGrain.SetNextGrain(countGrain);

                                //TODO: There is a bug below. It only connects those intermediary count grains to final grains which are used as an output by the currentGrain.
                                // Ideally this linking of grains within an operator should be done somewhere else.
                                ICountFinalOperatorGrain countFinalGrain = this.GrainFactory.GetGrain<ICountFinalOperatorGrain>(o.finalGrain.PrimaryKey, o.finalGrain.ExtensionKey);
                                await countGrain.SetNextGrain(countFinalGrain);
                                break;
                        }
                    }
                    
                }

                currentOperator = nextOperator;
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
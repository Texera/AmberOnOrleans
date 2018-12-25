using Orleans;
using System.Collections.Generic;
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
             Operator currentOperator;
             Operator nextOperator;

             currentOperator = workflow.StartOperator;
             while(currentOperator != null)
            {
                nextOperator = currentOperator.NextOperator;
                List<GrainIdentifier> currOpOutputGrainIDs = currentOperator.GetOutputGrainIDs();
            
                for(int i=0; i<currOpOutputGrainIDs.Count; i++)
                {
                    INormalGrain currGrain = null;
                    switch(currentOperator)
                    {
                        case ScanOperator o:
                            currGrain = this.GrainFactory.GetGrain<IScanOperatorGrain>(currOpOutputGrainIDs[i].PrimaryKey, currOpOutputGrainIDs[i].ExtensionKey);
                            await currGrain.TrivialCall();
                            break;
                        case FilterOperator o:
                            currGrain = this.GrainFactory.GetGrain<IFilterOperatorGrain>(currOpOutputGrainIDs[i].PrimaryKey, currOpOutputGrainIDs[i].ExtensionKey);
                            await currGrain.TrivialCall();
                            break;
                        case KeywordOperator o:
                            currGrain = this.GrainFactory.GetGrain<IKeywordSearchOperatorGrain>(currOpOutputGrainIDs[i].PrimaryKey, currOpOutputGrainIDs[i].ExtensionKey);
                            await currGrain.TrivialCall();
                            break;
                        case CountOperator o:
                            currGrain = this.GrainFactory.GetGrain<ICountFinalOperatorGrain>(currOpOutputGrainIDs[i].PrimaryKey, currOpOutputGrainIDs[i].ExtensionKey);
                            await currGrain.TrivialCall();
                            break;
                    }

                    await currGrain.SetPredicate(currentOperator.Predicate);

                    if(nextOperator != null)
                    {
                        List<GrainIdentifier> nextOpInputGrainIDs = nextOperator.GetInputGrainIDs();
                        GrainIdentifier nextGrainID = nextOpInputGrainIDs[i%nextOpInputGrainIDs.Count];
                        switch(nextOperator)
                        {
                            case ScanOperator o:
                                IScanOperatorGrain scanGrain = this.GrainFactory.GetGrain<IScanOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                                await scanGrain.TrivialCall();
                                await currGrain.SetNextGrain(scanGrain);
                                break;
                            case FilterOperator o:
                                IFilterOperatorGrain filterGrain = this.GrainFactory.GetGrain<IFilterOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                                await filterGrain.TrivialCall();
                                await currGrain.SetNextGrain(filterGrain);
                                break;
                            case KeywordOperator o:
                                IKeywordSearchOperatorGrain keywordGrain = this.GrainFactory.GetGrain<IKeywordSearchOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                                await keywordGrain.TrivialCall();
                                await currGrain.SetNextGrain(keywordGrain);
                                break;
                            case CountOperator o:
                                ICountOperatorGrain countGrain = this.GrainFactory.GetGrain<ICountOperatorGrain>(nextGrainID.PrimaryKey, nextGrainID.ExtensionKey);
                                await countGrain.TrivialCall();
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
                INormalGrain currGrain = null;
                switch(currentOperator)
                {   
                    case ScanOperator o:
                        currGrain = this.GrainFactory.GetGrain<IScanOperatorGrain>(currOpOutputGrainIDs[i].PrimaryKey, currOpOutputGrainIDs[i].ExtensionKey);
                        break;
                    case FilterOperator o:
                        currGrain = this.GrainFactory.GetGrain<IFilterOperatorGrain>(currOpOutputGrainIDs[i].PrimaryKey, currOpOutputGrainIDs[i].ExtensionKey);
                        break;
                    case KeywordOperator o:
                        currGrain = this.GrainFactory.GetGrain<IKeywordSearchOperatorGrain>(currOpOutputGrainIDs[i].PrimaryKey, currOpOutputGrainIDs[i].ExtensionKey);
                        break;
                    case CountOperator o:
                        currGrain = this.GrainFactory.GetGrain<ICountFinalOperatorGrain>(currOpOutputGrainIDs[i].PrimaryKey, currOpOutputGrainIDs[i].ExtensionKey);
                        break;
                }
                await currGrain.SetIsLastOperatorGrain(true);
            }

        }        
    }
}
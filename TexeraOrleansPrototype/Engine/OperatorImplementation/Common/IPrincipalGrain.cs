using Engine.Controller;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.SendingSemantics;
using Orleans.Core;
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Common
{
    public interface IPrincipalGrain : IGrainWithGuidKey
    {
        Task AddNextPrincipalGrain(IPrincipalGrain nextGrain);
        Task AddPrevPrincipalGrain(IPrincipalGrain prevGrain);
        Task Pause();
        Task Resume();
        Task Deactivate();
        Task Init(IControllerGrain controllerGrain, Guid workflowID, Operator currentOperator);
        Task<Dictionary<SiloAddress,List<IWorkerGrain>>> GetInputGrains();
        Task<ISendStrategy> GetInputSendStrategy(IGrain requester);
        Task<Dictionary<SiloAddress,List<IWorkerGrain>>> GetOutputGrains();
        Task LinkWorkerGrains();
        Task Start();


        //global count breakpoint: 
        Task SetBreakPoint(int targetValue);
        Task ReportCurrentValue(IGrain sender, int currentValue, int version);
    }
}
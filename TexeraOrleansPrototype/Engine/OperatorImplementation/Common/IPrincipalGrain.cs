using Engine.Controller;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.SendingSemantics;

namespace Engine.OperatorImplementation.Common
{
    public interface IPrincipalGrain : IGrainWithGuidKey
    {
        Task AddNextPrincipalGrain(IPrincipalGrain nextGrain);
        Task AddPrevPrincipalGrain(IPrincipalGrain prevGrain);
        Task Pause();
        Task Resume();
        Task Init(IControllerGrain controllerGrain, Guid workflowID, Operator currentOperator);
        Task<List<IWorkerGrain>> GetInputGrains();
        Task<ISendStrategy> GetInputSendStrategy();
        Task<List<IWorkerGrain>> GetOutputGrains();
        Task LinkWorkerGrains();
        Task Start();
    }
}
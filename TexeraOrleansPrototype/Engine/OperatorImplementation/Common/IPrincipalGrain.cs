using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Common
{
    public interface IPrincipalGrain : IGrainWithGuidCompoundKey
    {
        Task<Operator> GetOperator(); 
        Task SetOperator(Operator op);
        Task SetNextPrincipalGrain(IPrincipalGrain nextGrain);
        Task<IPrincipalGrain> GetNextPrincipalGrain();
        Task SetOperatorGrains(List<INormalGrain> operatorGrains);
        Task SetIsLastPrincipalGrain(bool isLastPrincipalGrain);
        Task<bool> GetIsLastPrincipalGrain();
        Task PauseGrain();
        Task ResumeGrain();
        Task SetUpAndConnectOperatorGrains();
        Task Init();
    }
}
using Orleans;
using System.Threading.Tasks;
using Engine.WorkflowImplementation;

namespace Engine.Controller
{
    public interface IControllerGrain : IGrainWithGuidCompoundKey
    {
        Task SetUpAndConnectGrains(Workflow workflow);
        Task CreateStreamFromLastOperator(Workflow workflow);
    }
}
using Orleans;
using System.Threading.Tasks;
using Engine.WorkflowImplementation;

namespace Engine.Controller
{
    public interface IControllerGrain : IGrainWithStringKey
    {
        Task Init(Workflow workflow);
    }
}
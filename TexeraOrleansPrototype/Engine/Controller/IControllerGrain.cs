using Orleans;
using System.Threading.Tasks;
using Engine.WorkflowImplementation;
using System.Collections.Generic;
using Engine.OperatorImplementation.Common;

namespace Engine.Controller
{
    public interface IControllerGrain : IGrainWithStringKey
    {
        Task Init(HashSet<Operator> graph);
    }
}
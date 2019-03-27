using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using Engine.Controller;
using Orleans;
using System.Threading.Tasks;

namespace Engine.WorkflowImplementation
{
    public class Workflow
    {
        public readonly HashSet<Operator> StartOperators = new HashSet<Operator>();
        public readonly HashSet<Operator> WorkflowGraph;
        public readonly HashSet<Operator> EndOperators=new HashSet<Operator>();
        public readonly string WorkflowID;
        public IControllerGrain workflowController=null;

        public Workflow(string workflowID, HashSet<Operator> allOperators)
        {
            this.WorkflowGraph=allOperators;
            this.WorkflowID=workflowID;
            foreach(Operator o in allOperators)
            {
                if(o.GetAllOutOperators().Count==0)
                    EndOperators.Add(o);
                if(o.IsStartOperator)
                    StartOperators.Add(o);
            }
        }

        public async Task Init(IGrainFactory factory)
        {
            workflowController=factory.GetGrain<IControllerGrain>(WorkflowID);
            await workflowController.Init(WorkflowGraph);
        }

        public async Task Pause()
        {
            foreach(Operator o in StartOperators)
            {
                await o.Pause();
            }
        }

        public async Task Resume()
        {
            foreach(Operator o in StartOperators)
            {
                await o.Resume();
            }
        }
    }
}
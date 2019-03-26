using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using Engine.Controller;

namespace Engine.WorkflowImplementation
{
    public class Workflow
    {
        public readonly HashSet<Operator> StartOperators = new HashSet<Operator>();
        public readonly HashSet<Operator> WorkflowGraph;
        public readonly HashSet<Operator> EndOperators=new HashSet<Operator>();
        public readonly string WorkflowID;
        public IControllerGrain workflowController;

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
    }
}
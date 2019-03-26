using System.Collections.Generic;
using Engine.OperatorImplementation.Common;

namespace Engine.WorkflowImplementation
{
    public class Workflow
    {
        public readonly List<Operator> StartOperators = new List<Operator>();
        public readonly List<Operator> WorkflowGraph;
        public readonly List<Operator> EndOperators=new List<Operator>();
        public readonly string WorkflowID;

        public Workflow(string workflowID, List<Operator> allOperators)
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
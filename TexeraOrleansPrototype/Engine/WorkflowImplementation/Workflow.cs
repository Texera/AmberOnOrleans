using Engine.OperatorImplementation.Common;

namespace Engine.WorkflowImplementation
{
    public class Workflow
    {
        public Operator StartOperator {get; set;}

        public Workflow(Operator startingOperator)
        {
            this.StartOperator = startingOperator;
        }
    }
}
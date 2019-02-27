using Engine.OperatorImplementation.Common;

namespace Engine.WorkflowImplementation
{
    public class Workflow
    {
        public Operator StartOperator {get; set;}

        public string WorkflowID {get; set;}

        public Workflow(Operator startingOperator)
        {
            this.StartOperator = startingOperator;
        }

        public Workflow()
        {

        }

        public Operator GetLastOperator()
        {
            Operator currentOperator;
            Operator nextOperator;

            currentOperator = StartOperator;
            nextOperator = currentOperator.NextOperator;

            while(nextOperator != null)
            {
                currentOperator = nextOperator;
                nextOperator = currentOperator.NextOperator;
            }
            
            return currentOperator;
        }
    }
}
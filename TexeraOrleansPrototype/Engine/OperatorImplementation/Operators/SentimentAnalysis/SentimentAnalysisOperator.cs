using Engine.OperatorImplementation.Common;
using Orleans;

namespace Engine.OperatorImplementation.Operators
{
    public class SentimentAnalysisOperator : Operator
    {
        
        public SentimentAnalysisOperator(SentimentAnalysisPredicate predicate) : base(predicate)
        {
            
        }

        public override void SetPrincipalGrain(IGrainFactory factory)
        {
            PrincipalGrain = factory.GetGrain<ISentimentAnalysisPrincipalGrain>(OperatorGuid);
        }
    }
}
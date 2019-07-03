using System.Collections.Generic;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class SentimentAnalysisPredicate : PredicateBase
    {
        public int PredictIndex;
        public SentimentAnalysisPredicate(int predictIndex, int batchingLimit=1000):base(batchingLimit)
        {
            PredictIndex=predictIndex;
        }
    }
}
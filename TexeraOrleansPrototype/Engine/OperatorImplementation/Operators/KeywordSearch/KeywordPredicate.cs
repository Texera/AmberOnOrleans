using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class KeywordPredicate : PredicateBase
    {
        public int SearchIndex;
        public string Query;

        public KeywordPredicate(int searchIndex, string query,int outputLimitPerBatch=-1,int batchingLimit=1000,int timeLimitPerBatch=-1):base(outputLimitPerBatch,batchingLimit,timeLimitPerBatch)
        {
            if(query == null)
            {
                query = "";
            }
            this.Query = query;
            this.SearchIndex=searchIndex;
        }
    }
}
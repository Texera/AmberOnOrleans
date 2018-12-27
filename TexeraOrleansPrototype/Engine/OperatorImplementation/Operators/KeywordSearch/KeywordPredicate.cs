using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class KeywordPredicate : PredicateBase
    {
        private string query;

        public KeywordPredicate(string query)
        {
            if(query == null)
            {
                query = "";
            }
            this.query = query;
        }

        public string GetQuery()
        {
            return query;
        }

        public override Operator GetNewOperator(int numberOfGrains)
        {
            return new KeywordOperator(this, numberOfGrains);
        }
    }
}
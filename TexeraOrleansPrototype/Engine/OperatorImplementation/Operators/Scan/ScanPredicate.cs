using Engine.OperatorImplementation.Common;
using Newtonsoft.Json.Linq;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanPredicate : PredicateBase
    {
        
        string file;
        public ScanPredicate(string file)
        {
            if(file == null)
            {
                file = "";
            }
            this.file = file;
        }

        public string GetFileName()
        {
            return file;
        }


        public override Operator GetNewOperator(int numberOfGrains)
        {
            return new ScanOperator(this, numberOfGrains);
        }
    }
}
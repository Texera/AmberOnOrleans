using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class FilterPredicate : PredicateBase
    {
        public int FilterIndex;
        public float Threshold;

        public enum FilterType
        {
            Equal,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual,
            NotEqual,
        }

        public FilterType Type;

        public FilterPredicate(int filterIndex, float threshold, string type, int batchingLimit=1000):base(batchingLimit)
        {
            this.Threshold = threshold;
            this.FilterIndex = filterIndex;
            switch(type)
            {
                case "=":
                    this.Type=FilterType.Equal;
                    break;
                case ">":
                    this.Type=FilterType.Greater;
                    break;
                case ">=":
                    this.Type=FilterType.GreaterOrEqual;
                    break;
                case "<":
                    this.Type=FilterType.Less;
                    break;
                case "<=":
                    this.Type=FilterType.LessOrEqual;
                    break;
                case "≠":
                    this.Type=FilterType.NotEqual;
                    break;
                default:
                    this.Type=FilterType.Equal;
                    break;
            }
        }

    }
}


namespace Engine.OperatorImplementation.Common
{
    public abstract class PredicateBase : IPredicate
    {
        public int BatchingLimit;
        public int OutputLimitPerBatch;
        public int TimeLimitPerBatch;

        public PredicateBase(int outputLimitPerBatch=-1,int batchingLimit=1000,int timeLimitPerBatch=3000)
        {
            BatchingLimit=batchingLimit;
            OutputLimitPerBatch=outputLimitPerBatch;
            TimeLimitPerBatch=timeLimitPerBatch;
        }
    }
}
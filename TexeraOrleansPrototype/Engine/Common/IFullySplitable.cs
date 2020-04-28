using System.Collections.Generic;

namespace Engine
{
    public interface IFullySplitable
    {
        List<object> FullySplit(int numPartitions);
    }
}
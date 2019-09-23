using System.Collections.Generic;

namespace Engine
{
    public interface IPartiallySplitable
    {
        List<object> PartiallySplit(int numPartitions);
    }
}
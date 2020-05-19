using System.Collections.Generic;

namespace Engine
{
    public interface IMergeable
    {
        void MergeWith(IEnumerable<object> list);

        void MergeWith(object obj);
    }
}
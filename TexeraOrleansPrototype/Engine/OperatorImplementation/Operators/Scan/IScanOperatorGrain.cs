using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public interface IScanOperatorGrain : INormalGrain
    {
        Task SubmitTuples();
        Task Init(ulong start_byte,ulong end_byte);
    }
}
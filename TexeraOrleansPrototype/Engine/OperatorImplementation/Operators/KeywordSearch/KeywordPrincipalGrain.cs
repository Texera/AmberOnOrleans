using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class KeywordPrinicipalGrain : PrincipalGrain, IKeywordPrincipalGrain
    {
        public override INormalGrain GetOperatorGrain(string extension)
        {
            return this.GrainFactory.GetGrain<IKeywordSearchOperatorGrain>(this.GetPrimaryKey(), extension);
        }
    }
}
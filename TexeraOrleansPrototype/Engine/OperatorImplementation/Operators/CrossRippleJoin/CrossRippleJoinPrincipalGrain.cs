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
    public class CrossRippleJoinPrinicipalGrain : PrincipalGrain, ICrossRippleJoinPrincipalGrain
    {
        public override int DefaultNumGrainsInOneLayer { get { return 1; } }
        public override async Task<IWorkerGrain> GetOperatorGrain(string extension)
        {
            var grain=this.GrainFactory.GetGrain<ICrossRippleJoinOperatorGrain>(this.GetPrimaryKey(), extension);
            await grain.Init(grain,predicate,self);
            return grain;
        }
    }
}
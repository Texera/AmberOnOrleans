using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;
using System.Linq;
using Engine.OperatorImplementation.SendingSemantics;

namespace Engine.OperatorImplementation.Operators
{
    public class HashJoinPrinicipalGrain : PrincipalGrain, IHashJoinPrincipalGrain
    {
        public override int DefaultNumGrainsInOneLayer { get { return 4; } }
        public override IWorkerGrain GetOperatorGrain(string extension)
        {
            return this.GrainFactory.GetGrain<IHashJoinOperatorGrain>(this.GetPrimaryKey(), extension);
        }

        public override Task<ISendStrategy> GetInputSendStrategy()
        {
            int joinFieldIndex=((HashJoinPredicate)predicate).JoinFieldIndex;
            return Task.FromResult(new Shuffle(inputGrains,Tuple=>1) as ISendStrategy);
        }
    }
}
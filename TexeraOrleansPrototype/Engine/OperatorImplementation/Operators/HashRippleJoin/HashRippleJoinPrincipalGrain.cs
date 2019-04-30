using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;
using System.Linq;
using System.Linq.Expressions;
using Engine.OperatorImplementation.SendingSemantics;
using Serialize.Linq.Extensions;
using Serialize.Linq.Serializers;

namespace Engine.OperatorImplementation.Operators
{
    public class HashRippleJoinPrinicipalGrain : PrincipalGrain, IHashRippleJoinPrincipalGrain
    {
        public override int DefaultNumGrainsInOneLayer { get { return 4; } }
        public override IWorkerGrain GetOperatorGrain(string extension)
        {
            return this.GrainFactory.GetGrain<IHashRippleJoinOperatorGrain>(this.GetPrimaryKey(), extension);
        }

        public override Task<ISendStrategy> GetInputSendStrategy()
        {
            int joinFieldIndex=((HashRippleJoinPredicate)predicate).JoinFieldIndex;
            Expression<Func<TexeraTuple,int>> exp=tuple=>tuple.FieldList[joinFieldIndex].GetHashCode();
            var serializer = new ExpressionSerializer(new JsonSerializer());
            return Task.FromResult(new Shuffle(inputGrains,serializer.SerializeText(exp)) as ISendStrategy);
        }
    }
}
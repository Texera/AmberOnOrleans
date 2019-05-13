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
    public class HashJoinPrinicipalGrain : PrincipalGrain, IHashJoinPrincipalGrain
    {
        public override int DefaultNumGrainsInOneLayer { get { return 6; } }
        public override async Task<IWorkerGrain> GetOperatorGrain(string extension)
        {
            var grain=this.GrainFactory.GetGrain<IHashJoinOperatorGrain>(this.GetPrimaryKey(), extension);
            await grain.Init(grain,predicate,self);
            return grain;
        }
        public override Task<ISendStrategy> GetInputSendStrategy(IGrain requester)
        {
            int joinFieldIndex;
            if(requester.GetPrimaryKey().Equals(((HashJoinPredicate)predicate).InnerTableID))
                joinFieldIndex=((HashJoinPredicate)predicate).InnerTableIndex;
            else
                joinFieldIndex=((HashJoinPredicate)predicate).OuterTableIndex;
            Expression<Func<TexeraTuple,int>> exp=tuple=>tuple.FieldList[joinFieldIndex].GetStableHashCode();
            var serializer = new ExpressionSerializer(new JsonSerializer());
            return Task.FromResult(new Shuffle(inputGrains,serializer.SerializeText(exp)) as ISendStrategy);
        }
    }
}
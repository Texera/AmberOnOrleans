using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;
using Engine.OperatorImplementation.SendingSemantics;
using System.Linq.Expressions;
using Serialize.Linq.Serializers;

namespace Engine.OperatorImplementation.Operators
{
    public class GroupByPrinicipalGrain : PrincipalGrain, IGroupByPrincipalGrain
    {
        public override IWorkerGrain GetOperatorGrain(string extension)
        {
            return this.GrainFactory.GetGrain<IGroupByOperatorGrain>(this.GetPrimaryKey(), extension);
        }

        public override Task<ISendStrategy> GetInputSendStrategy(IGrain requester)
        {
            int groupByIndex=((GroupByPredicate)predicate).GroupByIndex;
            Expression<Func<TexeraTuple,int>> exp=tuple=>tuple.FieldList[groupByIndex].GetHashCode();
            var serializer = new ExpressionSerializer(new JsonSerializer());
            return Task.FromResult(new Shuffle(inputGrains,serializer.SerializeText(exp)) as ISendStrategy);
        }
    }
}
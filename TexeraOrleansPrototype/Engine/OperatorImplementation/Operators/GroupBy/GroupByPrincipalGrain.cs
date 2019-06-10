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
using System.Linq;
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{
    public class GroupByPrinicipalGrain : PrincipalGrain, IGroupByPrincipalGrain
    {
        public override async Task BuildWorkerTopology()
        {
            //build backward
            //2-layer
            operatorGrains=Enumerable.Range(0, 2).Select(x=>new Dictionary<SiloAddress,List<IWorkerGrain>>()).ToList();
            //last layer
            for(int i=0;i<DefaultNumGrainsInOneLayer;++i)
            {
                IWorkerGrain finalGrain=this.GrainFactory.GetGrain<IGroupByFinalOperatorGrain>(this.GetPrimaryKey(),"final "+i);
                RequestContext.Clear();
                RequestContext.Set("excludeSilo",Constants.ClientIPAddress);
                RequestContext.Set("grainIndex",i);
                SiloAddress finalAddr=await finalGrain.Init(finalGrain,predicate,self);
                if(!operatorGrains[1].ContainsKey(finalAddr))
                {
                    operatorGrains[1].Add(finalAddr,new List<IWorkerGrain>{finalGrain});
                }
                else
                {
                    operatorGrains[1][finalAddr].Add(finalGrain);
                }
                //set target end flag
                await finalGrain.AddInputInformation(new Pair<Guid, int>(this.GetPrimaryKey(),DefaultNumGrainsInOneLayer));
            }            
            Expression<Func<TexeraTuple,int>> exp=tuple=>tuple.FieldList[0].GetStableHashCode();
            var serializer = new ExpressionSerializer(new JsonSerializer());
            ISendStrategy strategy=new Shuffle(serializer.SerializeText(exp),predicate.BatchingLimit);
            //first layer
            strategy.AddReceivers(operatorGrains[1].Values.SelectMany(x=>x).ToList());
            for(int i=0;i<DefaultNumGrainsInOneLayer;++i)
            {
                IWorkerGrain grain=this.GrainFactory.GetGrain<IGroupByOperatorGrain>(this.GetPrimaryKey(),i.ToString());
                RequestContext.Clear();
                RequestContext.Set("excludeSilo",Constants.ClientIPAddress);
                RequestContext.Set("grainIndex",i);
                SiloAddress addr=await grain.Init(grain,predicate,self);
                await grain.SetSendStrategy(this.GetPrimaryKey(),strategy);
                if(!operatorGrains[0].ContainsKey(addr))
                {
                    operatorGrains[0].Add(addr,new List<IWorkerGrain>{grain});
                }
                else
                {
                    operatorGrains[0][addr].Add(grain);
                }
            }
        }
    }
}
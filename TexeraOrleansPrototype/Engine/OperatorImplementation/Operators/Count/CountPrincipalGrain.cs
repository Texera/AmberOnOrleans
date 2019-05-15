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
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{
    public class CountPrinicipalGrain : PrincipalGrain, ICountPrincipalGrain
    {
        public override async Task BuildWorkerTopology()
        {
            //build backward
            //2-layer
            operatorGrains=Enumerable.Range(0, 2).Select(x=>new Dictionary<SiloAddress,List<IWorkerGrain>>()).ToList();
            //last layer
            IWorkerGrain finalGrain=this.GrainFactory.GetGrain<ICountFinalOperatorGrain>(this.GetPrimaryKey(),"final");
            SiloAddress finalAddr=await finalGrain.Init(finalGrain,predicate,self);
            operatorGrains[1].Add(finalAddr,new List<IWorkerGrain>{finalGrain});            
            //first layer
            ISendStrategy strategy=new RoundRobin();
            for(int i=0;i<DefaultNumGrainsInOneLayer;++i)
            {
                IWorkerGrain grain=this.GrainFactory.GetGrain<ICountOperatorGrain>(this.GetPrimaryKey(),i.ToString());
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
            //set target end flag
            await finalGrain.SetInputInformation(new Dictionary<Guid, int>{{this.GetPrimaryKey(),DefaultNumGrainsInOneLayer}});
        }
    }
}
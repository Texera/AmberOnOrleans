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
    public class CountPrinicipalGrain : PrincipalGrain, ICountPrincipalGrain
    {
        public override async void BuildWorkerTopology()
        {
            //build backward
            //2-layer
            operatorGrains=new List<List<IWorkerGrain>>(2);
            //last layer
            IWorkerGrain finalGrain=this.GrainFactory.GetGrain<ICountFinalOperatorGrain>(this.GetPrimaryKey(),"final");
            operatorGrains[1].Add(finalGrain);            
            //first layer
            for(int i=0;i<DefaultNumGrainsInOneLayer;++i)
            {
                IWorkerGrain grain=this.GrainFactory.GetGrain<ICountOperatorGrain>(this.GetPrimaryKey(),i.ToString());
                await grain.AddNextGrain(this.GetPrimaryKey(),finalGrain);
                operatorGrains[0].Add(grain);
            }
            //set target end flag
            await finalGrain.SetTargetEndFlagCount(DefaultNumGrainsInOneLayer);
        }
    }
}
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
    public class CountPrinicipalGrain : PrincipalGrain, IFilterPrincipalGrain
    {
        public override async Task Init(PredicateBase predicate)
        {
            for(int i=0;i<DefaultNumGrainsInOneLayer;++i)
            {
                INormalGrain grain=this.GrainFactory.GetGrain<ICountOperatorGrain>(this.GetPrimaryKey(),i.ToString());
                await grain.Init(predicate);
                inputGrains.Add(grain);
                operatorGrains.Add(grain);
            }
            INormalGrain finalGrain=this.GrainFactory.GetGrain<ICountFinalOperatorGrain>(this.GetPrimaryKey(),"final");
            outputGrains.Add(finalGrain);
            operatorGrains.Add(finalGrain);
        }
    }
}
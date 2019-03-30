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
    public class JoinPrinicipalGrain : PrincipalGrain, IFilterPrincipalGrain
    {
        public override int DefaultNumGrainsInOneLayer { get { return 1; } }
        public override IWorkerGrain GetOperatorGrain(string extension)
        {
            return this.GrainFactory.GetGrain<IJoinOperatorGrain>(this.GetPrimaryKey(), extension);
        }

        public override async Task BuildWorkerTopology()
        {
            base.BuildWorkerTopology();
        }
    }
}
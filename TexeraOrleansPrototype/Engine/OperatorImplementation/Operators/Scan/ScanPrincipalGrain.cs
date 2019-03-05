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
    public class ScanPrinicipalGrain : PrincipalGrain, IScanPrincipalGrain
    {
        public async Task StartScanGrain()
        {
            foreach(INormalGrain scanGrain in operatorGrains)
            {
                await StartScanOperatorGrain(0, (IScanOperatorGrain)scanGrain);
            }
        }

        private async Task StartScanOperatorGrain(int retryCount,IScanOperatorGrain grain)
        {
            grain.SubmitTuples().ContinueWith((t)=>
            {
                if(Engine.OperatorImplementation.Common.Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    grain.SubmitTuples();
            });
        }
    }
}
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
            Console.WriteLine($"Start method invoked in Scan Principal Grain");
            foreach(INormalGrain scanGrain in operatorGrains)
            {
                String extensionKey = "";
                Console.WriteLine($"Start method being sent to scan grain {scanGrain.GetPrimaryKey(out extensionKey)} - {extensionKey}");
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
using Engine.Breakpoint.GlobalBreakpoint;
using Engine.DeploySemantics;
using Engine.LinkSemantics;
using Engine.OperatorImplementation.Common;
using Orleans;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TexeraUtilities;

namespace Engine.OperatorImplementation.FaultTolerance
{
    public class HashBasedFolderScanOperator : Operator
    {
        private string folderName;
        private int numBuckets;
        public HashBasedFolderScanOperator(string folderName,int numBuckets) : base()
        {
            this.folderName = folderName;
            this.numBuckets = numBuckets;
        }

        public override void AssignBreakpoint(List<WorkerLayer> layers, Dictionary<IWorkerGrain, WorkerState> states, GlobalBreakpointBase breakpoint)
        {
            throw new System.NotImplementedException();
        }

        public override Pair<List<WorkerLayer>, List<LinkStrategy>> GenerateTopology()
        {
            return new Pair<List<WorkerLayer>,List<LinkStrategy>>
            (
                new List<WorkerLayer>
                {
                    new ProducerWorkerLayer("folder_scan.main",numBuckets,(i)=>new HashBasedFolderScanProducer(folderName+"/"+i,'|'),null)
                },
                new List<LinkStrategy>
                {
                    
                }
            );
        }

        public override string GetHashFunctionAsString(Guid from)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsStaged(Operator from)
        {
            return true;
        }
    }
}
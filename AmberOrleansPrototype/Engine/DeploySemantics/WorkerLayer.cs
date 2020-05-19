using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;
using Orleans.Runtime;

namespace Engine.DeploySemantics
{
    public abstract class WorkerLayer
    {
        public string id;
        protected Dictionary<String,Object> deployArgs;
        protected Dictionary<SiloAddress,List<IWorkerGrain>> layer = null;

        public Dictionary<SiloAddress,List<IWorkerGrain>> Layer
        {
            get{return layer;}
        }
        public WorkerLayer(string id,Dictionary<String,Object> deployArgs)
        {
            this.id = id;
            this.deployArgs = deployArgs;
        }

        public abstract Task Build(IPrincipalGrain principal, IGrainFactory gf, List<Pair<Operator,WorkerLayer>> prev);

        public bool IsBuilt
        {
            get{return layer != null;}
        }
    }
    
}

            

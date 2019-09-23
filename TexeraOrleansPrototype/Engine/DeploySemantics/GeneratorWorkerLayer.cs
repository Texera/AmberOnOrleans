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
using System.Linq;

namespace Engine.DeploySemantics
{
    public class ProducerWorkerLayer : WorkerLayer
    {
        private int numWorkers;
        private Func<int,ITupleProducer> workerGen;
        public ProducerWorkerLayer(string id, int numWorkers,Func<int,ITupleProducer> workerGen, Dictionary<String,Object> deployArgs):base(id,deployArgs)
        {
            this.numWorkers = numWorkers;
            this.workerGen = workerGen;
        }

        public override async Task Build(IPrincipalGrain principal, IGrainFactory gf, List<Pair<Operator, WorkerLayer>> prev)
        {
            layer = new Dictionary<SiloAddress, List<IWorkerGrain>>();
            for (int i = 0; i < numWorkers; ++i)
            {
                IWorkerGrain grain = gf.GetGrain<IWorkerGrain>(principal.GetPrimaryKey(),id+i.ToString());
                RequestContext.Clear();
                if(deployArgs!= null)
                {
                    foreach(var p in deployArgs)
                    {
                        RequestContext.Set(p.Key,p.Value);
                    }
                }
                RequestContext.Set("excludeSilo", Constants.ClientIPAddress);
                RequestContext.Set("grainIndex", i);
                SiloAddress addr = await grain.Init(principal,workerGen(i));
                Console.WriteLine("Placement: "+ OperatorImplementation.Common.Utils.GetReadableName(grain)+" placed at "+addr);
                if (!layer.ContainsKey(addr))
                {
                    layer.Add(addr, new List<IWorkerGrain> { grain });
                }
                else
                    layer[addr].Add(grain);
            }
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
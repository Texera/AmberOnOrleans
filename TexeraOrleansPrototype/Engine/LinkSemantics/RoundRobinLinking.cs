using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;
using Engine.DeploySemantics;
using System.Linq;
using Engine.OperatorImplementation.SendingSemantics;

namespace Engine.LinkSemantics
{
    public class RoundRobinLinking : LinkStrategy
    {
        string id;
        public RoundRobinLinking(WorkerLayer from, WorkerLayer to, int batchSize):base(from,to,batchSize)
        {
            this.id = from.id +">>"+to.id;
        }

        public override async Task Link()
        {
            List<IWorkerGrain> isolated=new List<IWorkerGrain>();
            foreach(var pair in to.Layer)
            {
                if(!from.Layer.ContainsKey(pair.Key))
                {
                    isolated.AddRange(pair.Value);
                }
            }
            foreach(var pair in from.Layer)
            {
                ISendStrategy strategy = new RoundRobin(batchSize);
                if(to.Layer.ContainsKey(pair.Key))
                {
                    strategy.AddReceivers(to.Layer[pair.Key],true);
                }
                else
                {
                    strategy.AddReceivers(to.Layer.Values.SelectMany(x=>x).ToList());
                }
                strategy.AddReceivers(isolated);
                foreach(IWorkerGrain grain in pair.Value)
                {
                    await grain.SetSendStrategy(id,strategy);
                }
            }
        }
    }

}
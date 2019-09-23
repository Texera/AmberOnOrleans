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
            ISendStrategy strategy = new RoundRobin(batchSize);
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
                List<IWorkerGrain> receivers=null;
                if(to.Layer.ContainsKey(pair.Key))
                {
                    receivers=to.Layer[pair.Key];
                    strategy.AddReceivers(receivers,true);
                }
                else
                {
                    receivers=to.Layer.Values.SelectMany(x=>x).ToList();
                    strategy.AddReceivers(receivers);
                }
                strategy.AddReceivers(isolated);
                receivers.AddRange(isolated);
                foreach(IWorkerGrain grain in pair.Value)
                {
                    await grain.SetSendStrategy(id,strategy);
                }
                strategy.RemoveAllReceivers();
            }
        }
    }

}
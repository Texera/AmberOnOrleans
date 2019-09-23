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
    public class OneToOneLinking : LinkStrategy
    {
        string id;
        public OneToOneLinking(WorkerLayer from, WorkerLayer to, int batchSize):base(from,to,batchSize)
        {
            this.id = from.id +">>"+to.id;
        }

        public override async Task Link()
        {
            if(from.Layer.Values.SelectMany(x=>x).Count() != to.Layer.Values.SelectMany(x=>x).Count())
            {
                throw new InvalidOperationException("OneToOne must be used between to layers with the same amount of nodes");
            }
            List<IWorkerGrain> unLinkedSender = new List<IWorkerGrain>();
            List<IWorkerGrain> unLinkedReceiver = new List<IWorkerGrain>();
            foreach(var fromPair in from.Layer)
            {
                if(to.Layer.ContainsKey(fromPair.Key))
                {
                    var receivers = to.Layer[fromPair.Key];
                    int limit = Math.Min(receivers.Count,fromPair.Value.Count);
                    for(int i=0;i<limit;++i)
                    {
                        var strategy = new OneToOne(batchSize);
                        strategy.AddReceiver(receivers[i],true);
                        await fromPair.Value[i].SetSendStrategy(id,strategy);
                    }
                    if(receivers.Count>limit)
                    {
                        unLinkedReceiver.AddRange(receivers.Skip(limit));
                    }
                    else if(fromPair.Value.Count>limit)
                    {
                        unLinkedSender.AddRange(fromPair.Value.Skip(limit));
                    }
                }
                else
                {
                    unLinkedSender.AddRange(fromPair.Value);
                }
            }
            foreach(var toPair in to.Layer)
            {
                if(!from.Layer.ContainsKey(toPair.Key))
                {
                    unLinkedReceiver.AddRange(toPair.Value);
                }
            }
            for(int i=0;i<unLinkedReceiver.Count;++i)
            {
                var strategy = new OneToOne(batchSize);
                strategy.AddReceiver(unLinkedReceiver[i],true);
                await unLinkedSender[i].SetSendStrategy(id,strategy);
            }
        }
    }

}
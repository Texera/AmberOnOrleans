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
    public class HashBasedShuffleLinking : LinkStrategy
    {
        string id;
        string jsonLambda;
        public HashBasedShuffleLinking(string jsonLambda, WorkerLayer from, WorkerLayer to, int batchSize):base(from,to,batchSize)
        {
            this.jsonLambda = jsonLambda;
            this.id = from.id +">>"+to.id;
        }

        public override async Task Link()
        {
            ISendStrategy strategy = new Shuffle(jsonLambda,batchSize);
            List<IWorkerGrain> receivers=to.Layer.Values.SelectMany(x=>x).ToList();
            List<IWorkerGrain> senders=from.Layer.Values.SelectMany(x=>x).ToList();
            foreach(var pair in from.Layer)
            {
                foreach(var receiver_pair in to.Layer)
                {
                    if(receiver_pair.Key.Equals(pair.Key))
                    {
                        strategy.AddReceivers(receiver_pair.Value,true);
                    }
                    else
                    {
                        strategy.AddReceivers(receiver_pair.Value);
                    }
                }
                foreach(IWorkerGrain grain in pair.Value)
                {
                    await grain.SetSendStrategy(id,strategy);
                }
                strategy.RemoveAllReceivers();
            }
        }
    }

}
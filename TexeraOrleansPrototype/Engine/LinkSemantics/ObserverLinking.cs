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
    public class ObserverLinking : LinkStrategy
    {
        string id;
        IAsyncStream<Immutable<PayloadMessage>> stream;
        public ObserverLinking(IAsyncStream<Immutable<PayloadMessage>> stream, WorkerLayer from, int batchSize = 100):base(from,null,batchSize)
        {
            this.id = from.id +">> frontend";
            this.stream = stream;
        }

        public override async Task Link()
        {
            List<IWorkerGrain> senders=from.Layer.Values.SelectMany(x=>x).ToList();
            foreach(IWorkerGrain grain in senders)
            {
                await grain.SetSendStrategy(id,new SendToStream(stream,batchSize));
            }
        }
    }

}
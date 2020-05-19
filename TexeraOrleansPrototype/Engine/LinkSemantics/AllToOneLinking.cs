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
    public class AllToOneLinking : LinkStrategy
    {
        string id;
        public AllToOneLinking(WorkerLayer from, WorkerLayer to, int batchSize):base(from,to,batchSize)
        {
            this.id = from.id +">>"+to.id;
        }

        public override async Task Link()
        {
            if(to.Layer.Values.SelectMany(x=>x).Count() != 1)
            {
                throw new InvalidOperationException("AllToOne must be used when there is a n-to-1 mapping");
            }
            var dest = to.Layer.First();
            foreach(var pair in from.Layer)
            {
                foreach(IWorkerGrain grain in pair.Value)
                {
                    var strategy = new OneToOne(batchSize);
                    strategy.AddReceiver(dest.Value.First(),pair.Key.Equals(dest.Key));
                    await grain.SetSendStrategy(id,strategy);
                }
            }
            
        }
    }

}
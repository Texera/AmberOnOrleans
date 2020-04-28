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

namespace Engine.LinkSemantics
{
    public abstract class LinkStrategy
    {

        protected WorkerLayer from;
        protected WorkerLayer to;
        protected int batchSize;

        public LinkStrategy(WorkerLayer from, WorkerLayer to, int batchSize)
        {
            this.from = from;
            this.to = to;
            this.batchSize = batchSize;
        }

        public abstract Task Link();
    }
    
}

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
    //Links actors of consecutive operators.
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

        //Link() function defines which worker actors of the next operator should be a receiver for an actor of the current operator.
        //Thus, Link() actually defines the SendStrategy() for each actor.
        public abstract Task Link();
    }
    
}

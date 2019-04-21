using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public abstract class MultiQueueBatching
    {
        private int batchingLimit;
        protected List<Queue<TexeraTuple>> outputRows;
        public MultiQueueBatching(int numReceivers,int batchingLimit=1000)
        {
            this.batchingLimit=batchingLimit;
            outputRows=Enumerable.Range(0,numReceivers).Select(x=>new Queue<TexeraTuple>()).ToList();
        }

        protected List<Pair<int,List<TexeraTuple>>> MakeBatchedPayloads()
        {
            List<Pair<int,List<TexeraTuple>>> result=new List<Pair<int, List<TexeraTuple>>>();
            for(int idx=0;idx<outputRows.Count;++idx)
            {
                Queue<TexeraTuple> queue=outputRows[idx];
                while(queue.Count>=batchingLimit)
                {
                    List<TexeraTuple> payload=new List<TexeraTuple>();
                    for(int j=0;j<batchingLimit;++j)
                    {
                        payload.Add(queue.Dequeue());
                    }
                    result.Add(new Pair<int,List<TexeraTuple>>(idx,payload));
                }
            }
            return result;
        }

        protected List<Pair<int,List<TexeraTuple>>> MakeLastPayload()
        {
            List<Pair<int,List<TexeraTuple>>> result=new List<Pair<int, List<TexeraTuple>>>();
            for(int i=0;i<outputRows.Count;++i)
            {
                if(outputRows[i].Count==0)continue;
                List<TexeraTuple> payload=new List<TexeraTuple>();
                payload.AddRange(outputRows[i]);
                outputRows[i]=new Queue<TexeraTuple>();
                result.Add(new Pair<int,List<TexeraTuple>>(i,payload));
            }
            return result;
        }
    }
}
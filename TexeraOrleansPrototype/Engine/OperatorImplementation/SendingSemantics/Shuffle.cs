using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using Serialize.Linq.Nodes;
using Serialize.Linq.Serializers;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public class Shuffle: MultiQueueSendStrategy
    {
        private string selectorExpression;
        private Func<TexeraTuple,int> selector=null;
        int localSender=0;
        public Shuffle(string jsonLambdaFunction, int batchingLimit=1000):base(batchingLimit)
        {
            this.selectorExpression=jsonLambdaFunction;
        }

        public override void Enqueue(List<TexeraTuple> output)
        {
            if(selector==null)
            {
                var serializer = new ExpressionSerializer(new JsonSerializer());
                var actualExpression = serializer.DeserializeText(selectorExpression);
                selector=((Expression<Func<TexeraTuple,int>>)actualExpression).Compile();
            }
            int limit=output.Count;
            int modlimit=outputRows.Count;
            int i=0;
            try
            {
                for(i=0;i<limit;++i)
                {
                    int idx=NonNegativeModular(selector(output[i]),modlimit);
                    outputRows[idx].Enqueue(output[i]);
                }
            }catch(Exception e)
            {
                Console.WriteLine("ERROR: "+String.Join(",",output[i].FieldList));
            }
        }

        public override void AddReceiver(IWorkerGrain receiver, bool localSending)
        {
            if(localSending)
            {
                localSender+=1;
                receivers.Add(new SendingUnit(receiver));
            }
            else
                receivers.Add(new FlowControlUnit(receiver));
            this.outputSequenceNumbers.Add(0);
            this.outputRows.Add(new Queue<TexeraTuple>());
        }

        public override void AddReceivers(List<IWorkerGrain> receivers, bool localSending)
        {
            if(localSending)
            {
                foreach(IWorkerGrain grain in receivers)
                {
                    localSender+=1;
                    this.receivers.Add(new SendingUnit(grain));
                }
            }
            else
            {
                foreach(IWorkerGrain grain in receivers)
                {
                    this.receivers.Add(new FlowControlUnit(grain));
                }
            }
            this.outputSequenceNumbers.AddRange(Enumerable.Repeat((ulong)0,receivers.Count));
            this.outputRows.AddRange(Enumerable.Range(0,receivers.Count).Select(x=>new Queue<TexeraTuple>()));
        }


        public override void SendBatchedMessages(IGrain senderIdentifier)
        {
            foreach(Pair<int,List<TexeraTuple>> pair in MakeBatchedPayloads())
            {
                receivers[pair.First].Send(new PayloadMessage(senderIdentifier,outputSequenceNumbers[pair.First]++,pair.Second,false));
            }
        }

        public override void SendEndMessages(IGrain senderIdentifier)
        {
            foreach(Pair<int,List<TexeraTuple>> pair in MakeLastPayload())
            {
                receivers[pair.First].Send(new PayloadMessage(senderIdentifier,outputSequenceNumbers[pair.First]++,pair.Second,false));
            }
            for(int i=0;i<receivers.Count;++i)
            {
                PayloadMessage message = new PayloadMessage(senderIdentifier,outputSequenceNumbers[i]++,null,true);
                receivers[i].Send(message);
            }
        }

        private int NonNegativeModular(int x, int m) {
            return (x%m + m)%m;
        }

        public override void RemoveAllReceivers()
        {
            receivers.Clear();
            this.outputSequenceNumbers.Clear();
            this.outputRows.Clear();
            localSender=0;
        }

        public override string ToString()
        {
            return "Shuffle: local = "+localSender+" non-local = "+(receivers.Count-localSender);
        }
    }
}
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
        public Shuffle(string jsonLambdaFunction, int batchingLimit=1000):base(batchingLimit)
        {
            this.selectorExpression=jsonLambdaFunction;
        }

        public override void Enqueue(IEnumerable<TexeraTuple> output)
        {
            if(selector==null)
            {
                var serializer = new ExpressionSerializer(new JsonSerializer());
                var actualExpression = serializer.DeserializeText(selectorExpression);
                selector=((Expression<Func<TexeraTuple,int>>)actualExpression).Compile();
            }
            foreach(TexeraTuple tuple in output)
            {
                int idx=NonNegativeModular(selector(tuple),outputRows.Count);
                outputRows[idx].Enqueue(tuple);
            }
        }

        public override void AddReceiver(IWorkerGrain receiver)
        {
            receivers.Add(receiver);
            this.outputSequenceNumbers.Add(0);
            this.outputRows.Add(new Queue<TexeraTuple>());
        }

        public override void AddReceivers(List<IWorkerGrain> receivers)
        {
            this.receivers.AddRange(receivers);
            this.outputSequenceNumbers.AddRange(Enumerable.Repeat((ulong)0,receivers.Count));
            this.outputRows.AddRange(Enumerable.Range(0,receivers.Count).Select(x=>new Queue<TexeraTuple>()));
        }


        public override async Task SendBatchedMessages(IGrain senderIdentifier)
        {
            foreach(Pair<int,List<TexeraTuple>> pair in MakeBatchedPayloads())
            {
                await SendMessageTo(receivers[pair.First],new PayloadMessage(senderIdentifier,outputSequenceNumbers[pair.First]++,pair.Second,false).AsImmutable(),0);
            }
        }

        public override async Task SendEndMessages(IGrain senderIdentifier)
        {
            foreach(Pair<int,List<TexeraTuple>> pair in MakeLastPayload())
            {
                await SendMessageTo(receivers[pair.First],new PayloadMessage(senderIdentifier,outputSequenceNumbers[pair.First]++,pair.Second,false).AsImmutable(),0);
            }
            for(int i=0;i<receivers.Count;++i)
            {
                Console.WriteLine(Utils.GetReadableName(senderIdentifier)+" -> "+ Utils.GetReadableName(receivers[i]) +" END: "+outputSequenceNumbers[i]);
                PayloadMessage message = new PayloadMessage(senderIdentifier,outputSequenceNumbers[i]++,null,true);
                await SendMessageTo(receivers[i],message.AsImmutable(),0);
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
        }
    }
}
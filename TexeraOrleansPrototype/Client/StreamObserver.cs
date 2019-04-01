using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Orleans.Concurrency;
using System.Diagnostics;
using TexeraUtilities;
using Engine;

namespace OrleansClient
{
    public class StreamObserver : IAsyncObserver<Immutable<PayloadMessage>>
    {
        public List<TexeraTuple> resultsToRet = new List<TexeraTuple>();
        Stopwatch sw=new Stopwatch();
        private Dictionary<string,ulong> endSequenceNumber=new Dictionary<string, ulong>();
        private Dictionary<string,ulong> currentSequenceNumber=new Dictionary<string, ulong>();
        private int numEndFlags;
        private int currentEndFlags=0;
        public bool isFinished=false;
        public Task Start()
        {
            sw.Start();
            return Task.CompletedTask;
        }

        public void SetNumEndFlags(int num)
        {
            numEndFlags=num;
        }

        public Task OnCompletedAsync()
        {
            Console.WriteLine("Chatroom message stream received stream completed event");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            Console.WriteLine($"Chatroom is experiencing message delivery failure, ex :{ex}");
            return Task.CompletedTask;
        }

        public Task OnNextAsync(Immutable<PayloadMessage> item, StreamSequenceToken token = null)
        {
            if(!currentSequenceNumber.ContainsKey(item.Value.SenderIdentifer))
            {
                currentSequenceNumber.Add(item.Value.SenderIdentifer,0);
            }
            if(item.Value.IsEnd)
            {
                endSequenceNumber.Add(item.Value.SenderIdentifer,item.Value.SequenceNumber);
                currentEndFlags++;
            }
            else
            {
                currentSequenceNumber[item.Value.SenderIdentifer]++;
                List<TexeraTuple> results = item.Value.Payload;
                resultsToRet.AddRange(results);
                //Console.WriteLine("Received "+results.Count+" tuples from the last operator");
            }
            if(currentEndFlags==numEndFlags && currentSequenceNumber.Count==endSequenceNumber.Count)
            {
                bool c=true;
                foreach(string s in currentSequenceNumber.Keys)
                {
                    if(currentSequenceNumber[s]!=endSequenceNumber[s])
                    {
                        c=false;
                        break;
                    }
                }
                if(c)
                {
                    isFinished=true;
                    sw.Stop();
                    Console.WriteLine("Time usage: " + sw.Elapsed);
                }
            }
            return Task.CompletedTask;
        }
    }
}
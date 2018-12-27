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

namespace OrleansClient
{
    public class StreamObserver : IAsyncObserver<Immutable<List<TexeraTuple>>>
    {
       public  List<TexeraTuple> resultsToRet = new List<TexeraTuple>();
        Stopwatch sw=new Stopwatch();

        public Task Start()
        {
            sw.Start();
            return Task.CompletedTask;
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

        public Task OnNextAsync(Immutable<List<TexeraTuple>> item, StreamSequenceToken token = null)
        {
            sw.Stop();
            Console.WriteLine("Time usage: " + sw.Elapsed);

            List<TexeraTuple> results = item.Value;
            resultsToRet.AddRange(results);
            for(int i=0; i<results.Count; i++)
            {
                Console.WriteLine($"=={results[i].seq_token}, {results[i].id}, {results[i].region}, {results[i].unit_cost}, {results[i].unit_price}, {results[i].units_sold}== count received: by client");
            }

            return Task.CompletedTask;
        }
    }
}
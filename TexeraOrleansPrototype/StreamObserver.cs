using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using System.Diagnostics;


namespace TexeraOrleansPrototype
{
    public class StreamObserver : IAsyncObserver<int>
    {
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

        public Task OnNextAsync(int item, StreamSequenceToken token = null)
        {
            sw.Stop();
            Console.WriteLine("Time usage: " + sw.Elapsed);
            Console.WriteLine($"=={item}== count received: by client");
            Environment.Exit(0);
            return Task.CompletedTask;
        }
    }
}
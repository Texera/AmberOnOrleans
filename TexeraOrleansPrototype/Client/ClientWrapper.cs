using Orleans;
using Orleans.Runtime;
using Orleans.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Engine.OperatorImplementation.Interfaces;
using TexeraUtilities;

namespace OrleansClient
{
    /// <summary>
    /// Orleans silo client
    /// </summary>
    public class ClientWrapper
    {
        const int initializeAttemptsBeforeFailing = 5;
        private static int attempt = 0;
        private static ClientWrapper instance;
        public IClusterClient client;
         

        public static ClientWrapper Instance
        {
            get 
            {
                if (instance == null)
                {
                    instance = new ClientWrapper();
                }
                return instance;
            }
        }

        private ClientWrapper()
        {
            try
            {
                client = StartClientWithRetries().Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task<IClusterClient> StartClientWithRetries()
        {
            attempt = 0;
            IClusterClient client;
            var clientBuilder = new ClientBuilder()
                    .UseLocalhostClustering()
                    .AddSimpleMessageStreamProvider("SMSProvider")
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "TexeraOrleansPrototype";
                    })
                    .ConfigureLogging(logging => logging.AddConsole());

            client = clientBuilder.Build();

            await client.Connect(RetryFilter);
            Console.WriteLine("Client successfully connect to silo host");
            return client;
        }

        private static async Task<bool> RetryFilter(Exception exception)
        {
            if (exception.GetType() != typeof(SiloUnavailableException))
            {
                Console.WriteLine($"Cluster client failed to connect to cluster with unexpected error.  Exception: {exception}");
                return false;
            }
            attempt++;
            Console.WriteLine($"Cluster client attempt {attempt} of {initializeAttemptsBeforeFailing} failed to connect to cluster.  Exception: {exception}");
            if (attempt > initializeAttemptsBeforeFailing)
            {
                return false;
            }
            await Task.Delay(TimeSpan.FromSeconds(4));
            return true;
        }

        public static async Task PauseSilo(IClusterClient client)
        {
            for (int i = 0; i < Constants.num_scan; ++i)
            {
                IScanOperator t = client.GetGrain<IScanOperator>(i + 2, Constants.AssemblyPath);
                await t.PauseGrain();
            }
        }

        public static async Task ResumeSilo(IClusterClient client)
        {
            for (int i = 0; i < Constants.num_scan; ++i)
            {
                IScanOperator t = client.GetGrain<IScanOperator>(i + 2, Constants.AssemblyPath);
                await t.ResumeGrain();
            }
        }

        public static async Task DoClientWork(IClusterClient client)
        {
            // example of calling grains from the initialized client
            // var friend = client.GetGrain<IHello>(0);
            // var response = await friend.SayHello("Good morning, my friend!");
            // Console.WriteLine("\n\n{0}\n\n", response);
            Guid streamGuid = await client.GetGrain<ICountFinalOperator>(1, Constants.AssemblyPath).GetStreamGuid();

            Console.WriteLine("Client side guid is " + streamGuid);
            var stream = client.GetStreamProvider("SMSProvider")
            .GetStream<int>(streamGuid, "Random");
            var so = new StreamObserver();
            await stream.SubscribeAsync(so);

            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine("Delivery: " + Constants.delivery);
            Console.WriteLine("# of workflows: " + Constants.num_scan);
            Console.WriteLine("FIFO & exactly-once: " + Constants.ordered_on);
            Console.WriteLine("dataset: " + Constants.dataset);
            Console.WriteLine("with conditions: " + Constants.conditions_on);
            Console.WriteLine();

            List<IScanOperator> operators = new List<IScanOperator>();
            for (int i = 0; i < Constants.num_scan; ++i)
            {
                var t = client.GetGrain<IScanOperator>(i + 2, Constants.AssemblyPath); //, "ScanOperatorWithSqNum"
                operators.Add(t);

                // Explicitly activating other grains
                await client.GetGrain<IFilterOperator>(i+2, Constants.AssemblyPath).TrivialCall(); //, "OrderedFilterOperatorWithSqNum"
                
                await client.GetGrain<IKeywordSearchOperator>(i+2, Constants.AssemblyPath).TrivialCall(); //, "OrderedKeywordSearchOperatorWithSqNum"
                
                await client.GetGrain<ICountOperator>(i+2, Constants.AssemblyPath).TrivialCall(); //, "OrderedCountOperatorWithSqNum"
                
                await client.GetGrain<ICountFinalOperator>(1, Constants.AssemblyPath).TrivialCall(); //, "OrderedCountFinalOperatorWithSqNum"
                
            }
            await Task.Delay(1000);
            Console.WriteLine("Start loading tuples");
            for (int i = 0; i < Constants.num_scan; ++i)
                await operators[i].LoadTuples();
            Console.WriteLine("Finish loading tuples");
            await so.Start();
            Console.WriteLine("Start experiment");
            for (int i = 0; i < Constants.num_scan; ++i)
            {
                operators[i].SubmitTuples();
            }
        }
    }
}
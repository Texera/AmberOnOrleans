using Orleans;
using Orleans.Runtime;
using Orleans.Hosting;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Engine.Controller;
using Engine.OperatorImplementation.Common;
using Engine.OperatorImplementation.Operators;
using Engine.WorkflowImplementation;
using TexeraUtilities;
using System.Threading;

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
        private Dictionary<string,Workflow> _IDToWorkflowEntry = new Dictionary<string,Workflow>();
        public Dictionary<string, Workflow> IDToWorkflowEntry { get => _IDToWorkflowEntry; }
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

        public static async Task PauseSilo(Workflow workflow, IClusterClient client)
        {
            HashSet<Operator> ops = workflow.StartOperators;
            foreach(Operator op in ops)
            {
                await op.PrincipalGrain.Pause();
            }
        }

        public static async Task ResumeSilo(Workflow workflow, IClusterClient client)
        {
            HashSet<Operator> ops = workflow.StartOperators;
            foreach(Operator op in ops)
            {
                await op.PrincipalGrain.Resume();
            }
        }

        public static async Task<List<TexeraTuple>> DoClientWork(IClusterClient client, Workflow workflow)
        {
            IControllerGrain controllerGrain = client.GetGrain<IControllerGrain>(workflow.WorkflowID);
            await controllerGrain.Init(workflow);
            var streamProvider = client.GetStreamProvider("SMSProvider");
            var so = new StreamObserver();
            foreach(Operator o in workflow.EndOperators)
            {
                var stream = streamProvider.GetStream<Immutable<List<TexeraTuple>>>(o.GetStreamGuid(), "OutputStream");
                await stream.SubscribeAsync(so);
            }
            instance.IDToWorkflowEntry[workflow.WorkflowID]=workflow;
            await so.Start();
            foreach(Operator op in workflow.StartOperators)
            {
                op.PrincipalGrain.Start();
            }

            while (so.resultsToRet.Count == 0)
            {

            }
            instance.IDToWorkflowEntry.Remove(workflow.WorkflowID);
            return so.resultsToRet;
        }

        private static List<IScanOperatorGrain> GetOperators(List<IScanOperatorGrain> operators)
        {
            return operators;
        }
    }
}
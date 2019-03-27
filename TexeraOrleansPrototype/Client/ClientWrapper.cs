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
        //singleton design
        private static ClientWrapper instance;
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


        public IClusterClient client;
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
        private Dictionary<string,Workflow> IDToWorkflowEntry = new Dictionary<string,Workflow>();
        private const int initializeAttemptsBeforeFailing = 5;
        private int attempt = 0;

        private async Task<IClusterClient> StartClientWithRetries()
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

        private async Task<bool> RetryFilter(Exception exception)
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

        public async Task PauseWorkflow(String workflowID)
        {
            if(IDToWorkflowEntry.ContainsKey(workflowID))
            {
                await IDToWorkflowEntry[workflowID].Pause();
            }
            else
            {
                throw new Exception("Workflow Not Found");
            }
        }

        public async Task ResumeWorkflow(String workflowID)
        {
            if(IDToWorkflowEntry.ContainsKey(workflowID))
            {
                await IDToWorkflowEntry[workflowID].Pause();
            }
            else
            {
                throw new Exception("Workflow Not Found");
            }
        }

        public async Task<List<TexeraTuple>> DoClientWork(IClusterClient client, Workflow workflow)
        {
            await workflow.Init(client);
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
    }
}
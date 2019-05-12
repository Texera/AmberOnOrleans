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
using System.Net;
using Engine;
using Orleans.Runtime.Placement;

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
        private Dictionary<Guid,Workflow> IDToWorkflowEntry = new Dictionary<Guid,Workflow>();
        private const int initializeAttemptsBeforeFailing = 5;
        private int attempt = 0;

        private async Task<IClusterClient> StartClientWithRetries()
        {
            attempt = 0;
            IClusterClient client;
            var clientBuilder = new ClientBuilder()
                    .UseAdoNetClustering(options =>
                    {
                        options.ConnectionString = Constants.connectionString;
                        options.Invariant = "MySql.Data.MySqlClient";
                    })
                    .AddSimpleMessageStreamProvider("SMSProvider")
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "TexeraOrleansPrototype";
                    })
                    .ConfigureServices(services => 
                    {
                        services.AddSingletonNamedService<PlacementStrategy, ScanPlacement>(nameof(ScanPlacement));
                        services.AddSingletonKeyedService<Type, IPlacementDirector, ScanPlacementDirector>(typeof(ScanPlacement));
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

        public async Task PauseWorkflow(Guid workflowID)
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

        public async Task ResumeWorkflow(Guid workflowID)
        {
            if(IDToWorkflowEntry.ContainsKey(workflowID))
            {
                await IDToWorkflowEntry[workflowID].Resume();
            }
            else
            {
                throw new Exception("Workflow Not Found");
            }
        }

        public async Task<List<TexeraTuple>> DoClientWork(IClusterClient client, Workflow workflow)
        {
            // code for testing the correctness of sequnece number:

            // IWorkerGrain grain=client.GetGrain<IWorkerGrain>(new Guid(),"2");
            // await grain.Init(grain,null,null);
            // List<ulong> seqnum=new List<ulong>{0,1,3,2,4,10,9,8,7,5,4,6};
            // foreach(ulong seq in seqnum)
            //     grain.ReceivePayloadMessage(new Immutable<PayloadMessage>(new PayloadMessage("123",seq,null,seq==10)));

            await workflow.Init(client);
            var streamProvider = client.GetStreamProvider("SMSProvider");
            var so = new StreamObserver();
            var stream = streamProvider.GetStream<Immutable<PayloadMessage>>(workflow.GetStreamGuid(), "OutputStream");
            var handle = await stream.SubscribeAsync(so);
            int numEndGrains=0;
            foreach(Operator o in workflow.EndOperators)
            {
                numEndGrains+=o.PrincipalGrain.GetOutputGrains().Result.Count;
            }
            so.SetNumEndFlags(numEndGrains);
            instance.IDToWorkflowEntry[workflow.WorkflowID]=workflow;
            await so.Start();
            foreach(Operator op in workflow.StartOperators)
            {
                Console.WriteLine("initing: "+op.GetType().ToString());
                await op.PrincipalGrain.Start();
            }

            while (!so.isFinished)
            {

            }
            await handle.UnsubscribeAsync();
            await workflow.Deactivate();
            instance.IDToWorkflowEntry.Remove(workflow.WorkflowID);
            return so.resultsToRet;
        }
    }
}
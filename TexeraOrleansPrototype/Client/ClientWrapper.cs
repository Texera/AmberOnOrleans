using Orleans;
using Orleans.Runtime;
using Orleans.Hosting;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Engine.Controller;
using Engine.OperatorImplementation.Common;
using Engine.OperatorImplementation.Operators;
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
        private Dictionary<Guid,IControllerGrain> IDToWorkflowEntry = new Dictionary<Guid,IControllerGrain>();
        private const int initializeAttemptsBeforeFailing = 5;
        private int attempt = 0;

        private async Task<IClusterClient> StartClientWithRetries()
        {
            attempt = 0;
            IClusterClient client;
            var clientBuilder = new ClientBuilder()
                    .UseAdoNetClustering(options =>
                    {
                        options.ConnectionString = Constants.ConnectionString;
                        options.Invariant = "MySql.Data.MySqlClient";
                    })
                    .AddSimpleMessageStreamProvider("SMSProvider")
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "TexeraOrleansPrototype";
                    })
                    .Configure<GatewayOptions>(options =>
                    {
                    })
                    .ConfigureServices(services => 
                    {
                        services.AddSingletonNamedService<PlacementStrategy, WorkerGrainPlacement>(nameof(WorkerGrainPlacement));
                        services.AddSingletonKeyedService<Type, IPlacementDirector, WorkerGrainPlacementDirector>(typeof(WorkerGrainPlacement));
                    })
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Configure<ClientMessagingOptions>(options => options.ResponseTimeout=new TimeSpan(0,10,0));

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

        public Task PauseWorkflow(Guid workflowID)
        {
            if(IDToWorkflowEntry.ContainsKey(workflowID))
            {
                IDToWorkflowEntry[workflowID].Pause();
            }
            else
            {
                throw new Exception("Workflow Not Found");
            }
            return Task.CompletedTask;
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

        public async Task<List<TexeraTuple>> DoClientWork(IClusterClient client, Guid workflowID, string plan)
        {
            // code for testing the correctness of sequnece number:

            // IWorkerGrain grain=client.GetGrain<IWorkerGrain>(Guid.NewGuid(),"2");
            // await grain.Init(grain,null,null);
            // List<ulong> seqnum=new List<ulong>{0,1,3,2,4,10,9,8,7,5,4,6};
            // foreach(ulong seq in seqnum)
            //     grain.ReceivePayloadMessage(new Immutable<PayloadMessage>(new PayloadMessage("123",seq,null,seq==10)));
            RequestContext.Set("targetSilo",Constants.ClientIPAddress);
            var deployGrain = client.GetGrain<IDeployGrain>(workflowID);
            var controllerGrain = await deployGrain.Init(workflowID,plan,false);
            var streamProvider = client.GetStreamProvider("SMSProvider");
            var so = new StreamObserver();
            var stream = streamProvider.GetStream<Immutable<PayloadMessage>>(controllerGrain.GetPrimaryKey(), "OutputStream");
            var handle = await stream.SubscribeAsync(so);
            so.SetNumEndFlags(await controllerGrain.GetNumberOfOutputGrains());
            instance.IDToWorkflowEntry[workflowID]=controllerGrain;
            // Operator secondScan=null;
            // foreach(Operator op in workflow.StartOperators)
            // {
            //     if(((ScanPredicate)op.Predicate).File.EndsWith("orders.tbl"))
            //         secondScan=op;
            // }
            // test count break point
            // foreach(Operator op in workflow.AllOperators)
            // {
            //     if(op is FilterOperator<DateTime>)
            //         await op.PrincipalGrain.SetCountBreakPoint(1000000);
            // }
            await so.Start();
            await controllerGrain.Start();
            // if(secondScan!=null)
            // {
            //     foreach(Operator op in workflow.StartOperators)
            //     {
            //         if(((ScanPredicate)op.Predicate).File.EndsWith("customer.tbl"))
            //         {
            //             await op.DelayPipeline(secondScan);
            //             await op.PrincipalGrain.Start();
            //         }
            //     }
            // }
            // else
            // {
            //     foreach(Operator op in workflow.StartOperators)
            //     {
            //         await op.PrincipalGrain.Start();
            //     }
            // }
            while (!so.isFinished)
            {

            }
            await handle.UnsubscribeAsync();
            await controllerGrain.Deactivate();
            instance.IDToWorkflowEntry.Remove(workflowID);
            return so.resultsToRet;
        }
    }
}
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
            Operator op = workflow.StartOperator;
            for (int i = 0; i < Constants.num_scan; ++i)
            {
                IScanOperatorGrain t = client.GetGrain<IScanOperatorGrain>(op.GetOperatorGuid(), i.ToString(), Constants.OperatorAssemblyPathPrefix);
                await t.PauseGrain();
            }
        }

        public static async Task ResumeSilo(Workflow workflow, IClusterClient client)
        {
            Operator op = workflow.StartOperator;
            for (int i = 0; i < Constants.num_scan; ++i)
            {
                IScanOperatorGrain t = client.GetGrain<IScanOperatorGrain>(op.GetOperatorGuid(), i.ToString(), Constants.OperatorAssemblyPathPrefix);
                await t.ResumeGrain();
            }
        }

        public static async Task<List<TexeraTuple>> DoClientWork(IClusterClient client, Workflow workflow)
        {
            // ScanPredicate scanPredicate = new ScanPredicate();
            // FilterPredicate filterPredicate = new FilterPredicate(0);
            // KeywordPredicate keywordPredicate = new KeywordPredicate("");
            // CountPredicate countPredicate = new CountPredicate();

            // ScanOperator scanOperator = (ScanOperator)scanPredicate.GetNewOperator(Constants.num_scan);
            // FilterOperator filterOperator = (FilterOperator)filterPredicate.GetNewOperator(Constants.num_scan);
            // KeywordOperator keywordOperator = (KeywordOperator)keywordPredicate.GetNewOperator(Constants.num_scan);
            // CountOperator countOperator = (CountOperator)countPredicate.GetNewOperator(Constants.num_scan);

            // scanOperator.NextOperator = filterOperator;
            // filterOperator.NextOperator = keywordOperator;
            // keywordOperator.NextOperator = countOperator;

            // Workflow workflow = new Workflow(scanOperator);



            ExecutionController controller = new ExecutionController(Guid.NewGuid());
            IControllerGrain controllerGrain = client.GetGrain<IControllerGrain>(controller.GrainID.PrimaryKey, controller.GrainID.ExtensionKey);
            await controllerGrain.SetUpAndConnectGrains(workflow);
            await controllerGrain.CreateStreamFromLastOperator(workflow);

            // Guid streamGuid = countOperator.GetStreamGuid();

            Guid streamGuid = workflow.GetLastOperator().GetStreamGuid();

            // Guid streamGuid = await client.GetGrain<ICountFinalOperatorGrain>(1, Constants.OperatorAssemblyPathPrefix).GetStreamGuid();

            Console.WriteLine("Client side guid is " + streamGuid);
            var stream = client.GetStreamProvider("SMSProvider")
            .GetStream<Immutable<List<TexeraTuple>>>(streamGuid, "Random");
            var so = new StreamObserver();
            await stream.SubscribeAsync(so);

            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine("Delivery: " + Constants.delivery);
            Console.WriteLine("# of workflows: " + Constants.num_scan);
            Console.WriteLine("FIFO & exactly-once: " + Constants.ordered_on);
            Console.WriteLine("with conditions: " + Constants.conditions_on);
            Console.WriteLine();

            // List<IScanOperatorGrain> operators = new List<IScanOperatorGrain>();
            // for (int i = 0; i < Constants.num_scan; ++i)
            // {
            //     var t = client.GetGrain<IScanOperatorGrain>(workflow.StartOperator.GetOperatorGuid(), i.ToString(), Constants.OperatorAssemblyPathPrefix); //, "ScanOperatorWithSqNum"
            //     operators.Add(t);
            // }
            Console.WriteLine("registered "+workflow.WorkflowID);
            instance.IDToWorkflowEntry[workflow.WorkflowID]=workflow;
            await so.Start();
            Console.WriteLine("Start experiment");

            ScanOperator scanOperator = (ScanOperator)workflow.StartOperator;
            IScanPrincipalGrain scanPrincipalGrain = client.GetGrain<IScanPrincipalGrain>(scanOperator.GetPrincipalGrainID().PrimaryKey, scanOperator.GetPrincipalGrainID().ExtensionKey);
            await scanPrincipalGrain.StartScanGrain();

            // for (int i = 0; i < Constants.num_scan; ++i)
            // {
            //     StartScanOperatorGrain(0, operators[i]);
            // }

            // Console.WriteLine("Pausing");
            // for (int i = 0; i < Constants.num_scan; ++i)
            // {
            //     await operators[i].PauseGrain();
            // }
            // Console.WriteLine("Paused");
            // Thread.Sleep(10000);
            // Console.WriteLine("Resuming");
            // for (int i = 0; i < Constants.num_scan; ++i)
            // {
            //     await operators[i].ResumeGrain();
            // }
            // Console.WriteLine("Resumed");
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

        // private static async void StartScanOperatorGrain(int retryCount,IScanOperatorGrain grain)
        // {
        //     grain.SubmitTuples().ContinueWith((t)=>
        //     {
        //         if(Engine.OperatorImplementation.Common.Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
        //             grain.SubmitTuples();
        //     });
        // }
    }
}
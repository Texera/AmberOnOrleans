using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Orleans;
using OrleansClient;
using TexeraUtilities;
namespace webapi
{
    public class Program
    {
        static IClusterClient client;

        public static int Main(string[] args)
        => CommandLineApplication.Execute<Program>(args);

        [Option("-c|--clientIP", Description = "IP address of client (MySQL server)")]
        public string ClientIPAddress { get; }=null;

        [Option("-r|--maxretries", Description = "number of max retries when message sending fails")]
        public int MaxRetries { get; }=-1;

        [Option("-b|--batchsize", Description = "number of tuples per batch")]
        public int BatchSize { get; }=-1;

        [Option("-n|--defaultlayersize", Description = "default number of grains per layer per operator")]
        public int DefaultNumGrainsInOneLayer{ get; }=-1;

        private void OnExecute()
        {
            if(DefaultNumGrainsInOneLayer!=-1)
            {
                Constants.DefaultNumGrainsInOneLayer=DefaultNumGrainsInOneLayer;
            }
            if(BatchSize!=-1)
            {
                Constants.BatchSize=BatchSize;
            }
            if(ClientIPAddress!=null)
            {
                Constants.ClientIPAddress=ClientIPAddress;
            }
            if(MaxRetries!=-1)
            {
                Constants.MaxRetries=MaxRetries;
            }
            Console.WriteLine("Ready to execute workflow :-) ");
            client=ClientWrapper.Instance.client;


            // BuildWebHost().Run();
            Console.WriteLine("Choose the workflow you would like to run - 1.TPCH-1, 2.TPCH-13");
            Console.WriteLine("Enter your option: (1 or 2)");
            
            String tpch1Workflow = "{\"logicalPlan\":{\"operators\":[{\"tableName\":\"{0}\",\"operatorID\":\"operator-348819d9-d8f7-4751-b9fb-60d84354c667\",\"operatorType\":\"ScanSource\"},{\"attributeName\":\"_c0\",\"attributeType\":\"string\",\"operatorID\":\"operator-20c3d1f3-bad1-4118-931a-4e28d95eaab1\",\"operatorType\":\"InsertionSort\"},{\"projectionAttributes\":\"_c0\",\"operatorID\":\"operator-9925727b-5a6c-4ce7-884d-34f9368bdfa8\",\"operatorType\":\"Projection\"}],\"links\":[{\"origin\":\"operator-348819d9-d8f7-4751-b9fb-60d84354c667\",\"destination\":\"operator-20c3d1f3-bad1-4118-931a-4e28d95eaab1\"},{\"origin\":\"operator-20c3d1f3-bad1-4118-931a-4e28d95eaab1\",\"destination\":\"operator-9925727b-5a6c-4ce7-884d-34f9368bdfa8\"}]},\"workflowID\":\"texera-workflow-d60c3150-bc78-4304-bf36-757aa7324ef1\"}";
            String tpch13Workflow = "{\"logicalPlan\":{\"operators\":[{\"tableName\":\"{0}\",\"operatorID\":\"operator-99971202-2931-4e95-8a0a-e3c249862077\",\"operatorType\":\"ScanSource\"},{\"tableName\":\"{1}\",\"operatorID\":\"operator-9c058413-8aa2-49d2-b155-a04ad224ddee\",\"operatorType\":\"ScanSource\"},{\"innerTableAttribute\":\"_c0\",\"outerTableAttribute\":\"_c1\",\"operatorID\":\"operator-1f71eded-c132-4cbc-9a7d-e0178014470c\",\"operatorType\":\"HashJoin\"},{\"groupByAttribute\":\"_c1\",\"aggregationAttribute\":\"_c0\",\"aggregationFunction\":\"count\",\"operatorID\":\"operator-81253cbd-79f7-42ee-b4af-735fc0a7c54c\",\"operatorType\":\"GroupBy\"},{\"aggregationFunction\":\"count\",\"operatorID\":\"operator-f22c633c-379b-4688-91b5-0fd67f984844\",\"operatorType\":\"GroupByFinal\"},{\"groupByAttribute\":\"_c1\",\"aggregationAttribute\":\"_c0\",\"aggregationFunction\":\"count\",\"operatorID\":\"operator-c7a490fa-9af7-4c86-b0c8-64186ee7c12e\",\"operatorType\":\"GroupBy\"},{\"aggregationFunction\":\"count\",\"operatorID\":\"operator-daa9d36b-6ffd-4efe-a73f-e522d9bf948f\",\"operatorType\":\"GroupByFinal\"},{\"attributeName\":\"_c0\",\"attributeType\":\"int\",\"operatorID\":\"operator-d05afadb-8870-4c27-9166-33a1dac23a83\",\"operatorType\":\"InsertionSort\"}],\"links\":[{\"origin\":\"operator-1f71eded-c132-4cbc-9a7d-e0178014470c\",\"destination\":\"operator-81253cbd-79f7-42ee-b4af-735fc0a7c54c\"},{\"origin\":\"operator-81253cbd-79f7-42ee-b4af-735fc0a7c54c\",\"destination\":\"operator-f22c633c-379b-4688-91b5-0fd67f984844\"},{\"origin\":\"operator-f22c633c-379b-4688-91b5-0fd67f984844\",\"destination\":\"operator-c7a490fa-9af7-4c86-b0c8-64186ee7c12e\"},{\"origin\":\"operator-c7a490fa-9af7-4c86-b0c8-64186ee7c12e\",\"destination\":\"operator-daa9d36b-6ffd-4efe-a73f-e522d9bf948f\"},{\"origin\":\"operator-daa9d36b-6ffd-4efe-a73f-e522d9bf948f\",\"destination\":\"operator-d05afadb-8870-4c27-9166-33a1dac23a83\"},{\"origin\":\"operator-99971202-2931-4e95-8a0a-e3c249862077\",\"destination\":\"operator-1f71eded-c132-4cbc-9a7d-e0178014470c\"},{\"origin\":\"operator-9c058413-8aa2-49d2-b155-a04ad224ddee\",\"destination\":\"operator-1f71eded-c132-4cbc-9a7d-e0178014470c\"}]},\"workflowID\":\"texera-workflow-3778d2b9-970c-413f-8315-47cb2ddb5f14\"}";

            String inputWorkflow = Console.ReadLine();
            String workflowChosen = inputWorkflow.Contains("1")?tpch1Workflow:tpch13Workflow;
            if(inputWorkflow.Contains("1"))
            {
                Console.WriteLine("Where is your lineitem table file? eg: D:\\\\lineitem.tbl");
                String file1 = Console.ReadLine();
                workflowChosen = workflowChosen.Replace("{0}",file1);
            } else {
                // implement for tpch-13
                Console.WriteLine("Where is your customer table file? eg: D:\\\\customer.tbl");
                String file1 = Console.ReadLine();
                workflowChosen = workflowChosen.Replace("{0}",file1);
                Console.WriteLine("Where is your orders table file? eg: D:\\\\orders.tbl");
                String file2 = Console.ReadLine();
                workflowChosen = workflowChosen.Replace("{1}",file2);
            }

            JObject o = JObject.Parse(workflowChosen);
            Guid workflowID;
            //remove "texera-workflow-" at the begining of workflowID to make it parsable to Guid
            if(!Guid.TryParse(o["workflowID"].ToString().Substring(16),out workflowID))
            {
                throw new Exception($"Parse workflowID failed! For {o["workflowID"].ToString().Substring(16)}");
            }
            List<TexeraTuple> results = ClientWrapper.Instance.DoClientWork(client, workflowID, o["logicalPlan"].ToString()).Result;
            if(results == null)
            {
                results = new List<TexeraTuple>();
            }
            
            Console.WriteLine("RESULTS:");
            results.ForEach(tuple=> {
                Console.WriteLine();
                for(int i=0; i<tuple.FieldList.Length; i++) {
                    Console.Write(tuple.FieldList[i]+",");
                }
                Console.WriteLine();
            });
        }

        public static IWebHost BuildWebHost() =>
            WebHost.CreateDefaultBuilder()
                .UseWebRoot("../Frontend/dist")
                .UseStartup<Startup>()
                .UseUrls("http://*:7070")
                .Build();
    }
}

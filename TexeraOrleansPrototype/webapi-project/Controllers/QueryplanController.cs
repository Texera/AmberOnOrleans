using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Orleans.Hosting;
using OrleansClient;
using SiloHost;
using Engine.WorkflowImplementation;
using Engine.OperatorImplementation.Common;
using Engine.OperatorImplementation.Operators;
using TexeraUtilities;

namespace webapi.Controllers
{
    // [Route("api/[controller]")]
    public class QueryplanController : Controller
    {
        private static ISiloHost host;
        private static IClusterClient client;

        [HttpPost]
        [Route("api/queryplan/execute")]
        public IActionResult Execute([FromBody]string logicalPlanJson)
        {
            if(host == null)
            {
                host = SiloWrapper.Instance.host;
            }
            
            if(client == null)
            {
                client = ClientWrapper.Instance.client;
            }

            Stream req = Request.Body;
            req.Seek(0, System.IO.SeekOrigin.Begin);
            string json = new StreamReader(req).ReadToEnd();

            Console.WriteLine("JSON BODY = " + json);
            Dictionary<string, Operator> map = new Dictionary<string, Operator>();
            Workflow workflow = new Workflow();

            JObject o = JObject.Parse(json);
            JArray operators = (JArray)o["operators"];
            
            foreach (JObject operator1 in operators)
            {
                if((string)operator1["operatorType"] == "ScanSource")
                {
                    Console.WriteLine("Scan");
                    //example path to HDFS through WebHDFS API: "http://localhost:50070/webhdfs/v1/input/very_large_input.csv"
                    ScanPredicate scanPredicate = new ScanPredicate((string)operator1["tableName"]);
                    ScanOperator scanOperator = (ScanOperator)scanPredicate.GetNewOperator(Constants.num_scan);
                    map.Add((string)operator1["operatorID"], scanOperator);
                    workflow.StartOperator = scanOperator;
                }
                else if((string)operator1["operatorType"] == "KeywordMatcher")
                {
                    Console.WriteLine("Keyword - " + (string)operator1["query"]);
                    KeywordPredicate keywordPredicate = new KeywordPredicate((string)operator1["query"]);
                    KeywordOperator keywordOperator = (KeywordOperator)keywordPredicate.GetNewOperator(Constants.num_scan);
                    map.Add((string)operator1["operatorID"], keywordOperator);
                }
                else if((string)operator1["operatorType"] == "Aggregation")
                {
                    Console.WriteLine("Count");
                    CountPredicate countPredicate = new CountPredicate();
                    CountOperator countOperator = (CountOperator)countPredicate.GetNewOperator(Constants.num_scan);
                    map.Add((string)operator1["operatorID"], countOperator);
                }
                else if((string)operator1["operatorType"] == "Comparison")
                {
                    Console.WriteLine("Filter - " + (string)operator1["compareTo"]);
                    FilterPredicate filterPredicate = new FilterPredicate((int)operator1["compareTo"]);
                    FilterOperator filterOperator = (FilterOperator)filterPredicate.GetNewOperator(Constants.num_scan);
                    map.Add((string)operator1["operatorID"], filterOperator);
                }
            }

            JArray links = (JArray)o["links"];
            foreach(JObject link in links)
            {
                Operator origin = map[(string)link["origin"]];
                Operator dest = map[(string)link["destination"]];
                origin.NextOperator = dest;
            }

            List<TexeraTuple> results = ClientWrapper.DoClientWork(client, workflow).Result;
            TexeraResult texeraResult = new TexeraResult();
            texeraResult.code = 0;
            if(results == null)
            {
                results = new List<TexeraTuple>();
            }
            texeraResult.result = results;
            texeraResult.resultID = Guid.NewGuid();

            return Json(texeraResult);
        }
    }
}
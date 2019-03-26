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

            JObject o = JObject.Parse(json);
            JArray operators = (JArray)o["logicalPlan"]["operators"];
            int table_id=0;
            foreach (JObject operator1 in operators)
            {
                Operator op=null;
                if((string)operator1["operatorType"] == "ScanSource")
                {
                    //example path to HDFS through WebHDFS API: "http://localhost:50070/webhdfs/v1/input/very_large_input.csv"
                    ScanPredicate scanPredicate = new ScanPredicate((string)operator1["tableName"],table_id++);
                    op = new ScanOperator(scanPredicate);
                }
                else if((string)operator1["operatorType"] == "KeywordMatcher")
                {
                    KeywordPredicate keywordPredicate = new KeywordPredicate((string)operator1["query"]);
                    op = new KeywordOperator(keywordPredicate);
                }
                else if((string)operator1["operatorType"] == "Aggregation")
                {
                    CountPredicate countPredicate = new CountPredicate();
                    op = new CountOperator(countPredicate);
                }
                else if((string)operator1["operatorType"] == "Comparison")
                {
                    FilterPredicate filterPredicate = new FilterPredicate((int)operator1["compareTo"]);
                    op = new FilterOperator(filterPredicate);
                }
                if(op!=null)
                    map.Add((string)operator1["operatorID"],op);
            }

            JArray links = (JArray)o["logicalPlan"]["links"];
            foreach(JObject link in links)
            {
                Operator origin = map[(string)link["origin"]];
                Operator dest = map[(string)link["destination"]];
                origin.AddOutOperator(dest);
            }

            Workflow workflow=new Workflow(o["workflowID"].ToString(),new HashSet<Operator>(map.Values));

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
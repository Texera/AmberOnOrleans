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
            Guid workflowID;
            //remove "texera-workflow-" at the begining of workflowID to make it parsable
            if(!Guid.TryParse(o["workflowID"].ToString().Substring(16),out workflowID))
            {
                throw new Exception($"Parse workflowID failed! For {o["workflowID"].ToString().Substring(16)}");
            }
            Workflow workflow=new Workflow(workflowID);
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
                    KeywordPredicate keywordPredicate = new KeywordPredicate(int.Parse(operator1["attributeName"].ToString().Replace("_c","")),operator1["keyword"].ToString());
                    op = new KeywordOperator(keywordPredicate);
                }
                else if((string)operator1["operatorType"] == "Aggregation")
                {
                    CountPredicate countPredicate = new CountPredicate();
                    op = new CountOperator(countPredicate);
                }
                else if((string)operator1["operatorType"] == "Comparison")
                {
                    FilterPredicate filterPredicate = new FilterPredicate(int.Parse(operator1["attributeName"].ToString().Replace("_c","")),float.Parse(operator1["compareTo"].ToString()),operator1["comparisonType"].ToString());
                    op = new FilterOperator(filterPredicate);
                }
                else if((string)operator1["operatorType"] == "CrossRippleJoin")
                {
                    int outputLimit=operator1["outputLimitPerBatch"]==null?-1:int.Parse(operator1["outputLimitPerBatch"].ToString());
                    int inputLimit=operator1["batchingLimit"]==null?1000:int.Parse(operator1["batchingLimit"].ToString());
                    int timeLimit=operator1["timeLimitPerBatch(ms)"]==null?-1:int.Parse(operator1["timeLimitPerBatch(ms)"].ToString());
                    JoinPredicate joinPredicate=new JoinPredicate(table_id++,inputLimit);
                    op = new JoinOperator(joinPredicate);
                }
                else if((string)operator1["operatorType"] == "HashRippleJoin")
                {
                    HashJoinPredicate hashJoinPredicate=new HashJoinPredicate(int.Parse(operator1["attributeName"].ToString().Replace("_c","")),table_id++);
                    op = new HashJoinOperator(hashJoinPredicate);
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
                dest.AddInOperator(origin);
            }

            workflow.InitializeOperatorSet(new HashSet<Operator>(map.Values));

            List<TexeraTuple> results = ClientWrapper.Instance.DoClientWork(client, workflow).Result;
            if(results == null)
            {
                results = new List<TexeraTuple>();
            }
            List<JObject> resultJson=new List<JObject>();
            foreach(TexeraTuple tuple in results)
            {
                JObject jsonTuple=new JObject();
                jsonTuple.Add("TableID",tuple.TableID);
                for(int i=0;i<tuple.FieldList.Length;++i)
                {
                    jsonTuple.Add("_c"+i,tuple.FieldList[i]);
                }
                resultJson.Add(jsonTuple);
            }
            TexeraResult texeraResult = new TexeraResult();
            texeraResult.code = 0;
            texeraResult.result = resultJson;
            texeraResult.resultID = Guid.NewGuid();

            return Json(texeraResult);
        }
    }
}
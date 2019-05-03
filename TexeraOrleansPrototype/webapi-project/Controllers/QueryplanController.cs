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
                    KeywordPredicate keywordPredicate = new KeywordPredicate(int.Parse(operator1["attributeName"].ToString().Replace("_c","")),operator1["keyword"]!=null?operator1["keyword"].ToString():"");
                    op = new KeywordOperator(keywordPredicate);
                }
                else if((string)operator1["operatorType"] == "Aggregation")
                {
                    CountPredicate countPredicate = new CountPredicate();
                    op = new CountOperator(countPredicate);
                }
                else if((string)operator1["operatorType"] == "Comparison")
                {
                    FilterPredicate filterPredicate = new FilterPredicate(int.Parse(operator1["attributeName"].ToString().Replace("_c","")),operator1["compareTo"].ToString(),operator1["comparisonType"].ToString());
                    switch(operator1["attributeType"].ToString())
                    {
                        case "int":
                            op = new FilterOperator<int>(filterPredicate);
                            break;
                        case "float":
                            op = new FilterOperator<float>(filterPredicate);
                            break;
                        case "date":
                            op=new FilterOperator<DateTime>(filterPredicate);
                            break;
                        case "string":
                            op=new FilterOperator<string>(filterPredicate);
                            break;
                    }
                }
                else if((string)operator1["operatorType"] == "CrossRippleJoin")
                {
                    int inputLimit=operator1["batchingLimit"]==null?1000:int.Parse(operator1["batchingLimit"].ToString());
                    CrossRippleJoinPredicate crossRippleJoinPredicate=new CrossRippleJoinPredicate(table_id++,inputLimit);
                    op = new CrossRippleJoinOperator(crossRippleJoinPredicate);
                }
                else if((string)operator1["operatorType"] == "HashRippleJoin")
                {
                    HashRippleJoinPredicate hashRippleJoinPredicate=new HashRippleJoinPredicate(int.Parse(operator1["attributeName"].ToString().Replace("_c","")),table_id++);
                    op = new HashRippleJoinOperator(hashRippleJoinPredicate);
                }
                else if((string)operator1["operatorType"] == "InsertionSort")
                {
                    SortPredicate sortPredicate=new SortPredicate(int.Parse(operator1["attributeName"].ToString().Replace("_c","")));
                    switch(operator1["attributeType"].ToString())
                    {
                        case "int":
                            op = new SortOperator<int>(sortPredicate);
                            break;
                        case "float":
                            op = new SortOperator<float>(sortPredicate);
                            break;
                        case "date":
                            op= new SortOperator<DateTime>(sortPredicate);
                            break;
                        case "string":
                            op= new SortOperator<string>(sortPredicate);
                            break;
                    }
                }
                else if((string)operator1["operatorType"] == "GroupBy")
                {
                    int groupByIndex=int.Parse(operator1["groupByAttribute"].ToString().Replace("_c",""));
                    int aggregationIndex=int.Parse(operator1["aggregationAttribute"].ToString().Replace("_c",""));
                    GroupByPredicate groupByPredicate=new GroupByPredicate(groupByIndex,aggregationIndex,operator1["aggregationFunction"].ToString());
                    op=new GroupByOperator(groupByPredicate);
                }
                else if((string)operator1["operatorType"] == "Projection")
                {
                    List<int> projectionIndexs=operator1["projectionAttributes"].ToString().Split(",").Select(x=>int.Parse(x.Replace("_c",""))).ToList();
                    ProjectionPredicate projectionPredicate=new ProjectionPredicate(projectionIndexs);
                    op=new ProjectionOperator(projectionPredicate);
                }
                else if((string)operator1["operatorType"] == "HashJoin")
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
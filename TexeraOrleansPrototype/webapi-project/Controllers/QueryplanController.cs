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
using Engine.OperatorImplementation.Common;
using Engine.OperatorImplementation.Operators;
using TexeraUtilities;

namespace webapi.Controllers
{
    // [Route("api/[controller]")]
    public class QueryplanController : Controller
    {
        private static IClusterClient client;

        [HttpPost]
        [Route("api/queryplan/execute")]
        public IActionResult Execute([FromBody]string logicalPlanJson)
        {
            
            if(client == null)
            {
                client = ClientWrapper.Instance.client;
            }

            //Stream req = Request.Body;
            //req.Seek(0, System.IO.SeekOrigin.Begin);
            string json = logicalPlanJson;
            //apply TPC-H Query-1
            //json="{\"logicalPlan\":{\"operators\":[{\"tableName\":\"D:\\\\median_input.csv\",\"operatorID\":\"operator-348819d9-d8f7-4751-b9fb-60d84354c667\",\"operatorType\":\"ScanSource\"},{\"attributeName\":\"_c0\",\"attributeType\":\"string\",\"operatorID\":\"operator-20c3d1f3-bad1-4118-931a-4e28d95eaab1\",\"operatorType\":\"InsertionSort\"},{\"projectionAttributes\":\"_c0\",\"operatorID\":\"operator-9925727b-5a6c-4ce7-884d-34f9368bdfa8\",\"operatorType\":\"Projection\"}],\"links\":[{\"origin\":\"operator-348819d9-d8f7-4751-b9fb-60d84354c667\",\"destination\":\"operator-20c3d1f3-bad1-4118-931a-4e28d95eaab1\"},{\"origin\":\"operator-20c3d1f3-bad1-4118-931a-4e28d95eaab1\",\"destination\":\"operator-9925727b-5a6c-4ce7-884d-34f9368bdfa8\"}]},\"workflowID\":\"texera-workflow-d60c3150-bc78-4304-bf36-757aa7324ef1\"}";
            Console.WriteLine("JSON BODY = " + json);
            JObject o = JObject.Parse(json);
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
            List<JObject> resultJson=new List<JObject>();
            foreach(TexeraTuple tuple in results)
            {
                JObject jsonTuple=new JObject();
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
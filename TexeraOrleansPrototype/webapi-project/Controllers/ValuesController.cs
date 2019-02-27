using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Engine.WorkflowImplementation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Hosting;
using OrleansClient;
using SiloHost;

namespace webapi.Controllers
{
    //[Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private static ISiloHost host;
        private static IClusterClient client;
        public ValuesController()
        {
            if(host == null)
            {
                host = SiloWrapper.Instance.host;
            }
            
            if(client == null)
            {
                client = ClientWrapper.Instance.client;
            }
        }

        //Post api/pause
        [HttpPost]
        [Route("api/pause")]
        public async Task<HttpResponseMessage> PostPause()
        {
            Console.WriteLine("action: pause");
            Stream req = Request.Body;
            //req.Seek(0, System.IO.SeekOrigin.Begin);
            string json = new StreamReader(req).ReadToEnd();
            JObject o = JObject.Parse(json);
            string workflowID = o["workflowID"].ToString();
            Console.WriteLine("target: "+workflowID);
            Workflow workflow;
            if(ClientWrapper.Instance.IDToWorkflowEntry.ContainsKey(workflowID))
                workflow = ClientWrapper.Instance.IDToWorkflowEntry[workflowID];
            else
            {
                Console.WriteLine("but not found!");
                throw new Exception();
            }
            await ClientWrapper.PauseSilo(workflow,client);
            Console.WriteLine("Paused!");
            return new HttpResponseMessage();
        }


        //Post api/pause
        [HttpPost]
        [Route("api/resume")]
        public async Task<HttpResponseMessage> PostResume()
        {
            Console.WriteLine("action: resume");
            Stream req = Request.Body;
            //req.Seek(0, System.IO.SeekOrigin.Begin);
            string json = new StreamReader(req).ReadToEnd();
            JObject o = JObject.Parse(json);
            string workflowID = o["workflowID"].ToString();
            Console.WriteLine("target: "+workflowID);
            Workflow workflow;
            if(ClientWrapper.Instance.IDToWorkflowEntry.ContainsKey(workflowID))
                workflow = ClientWrapper.Instance.IDToWorkflowEntry[workflowID];
            else
            {
                Console.WriteLine("but not found!");
                throw new Exception();
            }
            await ClientWrapper.ResumeSilo(workflow,client);
            Console.WriteLine("Resumed!");
            return new HttpResponseMessage();
        }
    }
}

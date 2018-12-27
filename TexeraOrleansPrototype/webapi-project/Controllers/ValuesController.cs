using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Orleans.Hosting;
using OrleansClient;
using SiloHost;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private static ISiloHost host;
        private static IClusterClient client;

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            if(host == null)
            {
                host = SiloWrapper.Instance.host;
            }
            
            if(client == null)
            {
                client = ClientWrapper.Instance.client;
            }

            // ClientWrapper.DoClientWork(client).Wait();

            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            if(id==5)
            {
                // ClientWrapper.PauseSilo(client);
                return "Paused";
            }
            if(id==6)
            {
                // ClientWrapper.ResumeSilo(client);
                return "Resumed";
            }

            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
            // ClientWrapper.ResumeSilo(client);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

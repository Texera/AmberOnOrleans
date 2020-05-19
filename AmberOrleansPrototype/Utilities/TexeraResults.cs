using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;

namespace TexeraUtilities
{
    public class TexeraResult
    {
        public int code {get; set;}
        public List<JObject> result {get; set;}
        public Guid resultID {get; set;}

    }
}
using System.Collections.Generic;
using System;

namespace TexeraUtilities
{
    public class TexeraResult
    {
        public int code {get; set;}
        public List<TexeraTuple> result {get; set;}
        public Guid resultID {get; set;}

    }
}
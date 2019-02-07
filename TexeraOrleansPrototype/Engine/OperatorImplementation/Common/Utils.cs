using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Engine.OperatorImplementation.MessagingSemantics;
using System.Text;
using System.IO;

namespace Engine.OperatorImplementation.Common
{
    static public class Utils
    {
        static public IOrderingEnforcer GetOrderingEnforcerInstance()
        {
            return new OrderingGrainWithSequenceNumber();
            // return new OrderingGrainWithContinuousSending();
        }

        static public string GenerateURLForHDFSWebAPI(string filename,ulong offset)
        {
            StringBuilder sb=new StringBuilder();
            sb.Append(filename);
            sb.Append("?op=OPEN&offset=");
            sb.Append(offset);
            return sb.ToString();
        }

        static public StreamReader GetFileHandleFromHDFS(string uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream(); 
                StreamReader reader = new StreamReader(stream);
                return reader;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        
    }
}
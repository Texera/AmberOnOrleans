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
using Engine.OperatorImplementation.Operators;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using TexeraUtilities;

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


        static public ulong GetFileLengthFromHDFS(string filename)
        {
            StringBuilder sb=new StringBuilder();
            sb.Append(filename);
            sb.Append("?op=GETFILESTATUS");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sb.ToString());
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader Reader = new StreamReader(response.GetResponseStream()))
            {
                string str_response=Reader.ReadToEnd();
                Console.WriteLine(str_response);
                JObject obj =JObject.Parse(str_response);
                return UInt64.Parse(obj["FileStatus"]["length"].ToString());
            }
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

        static public bool IsTaskTimedOut(Task t)
        {
            if (t.IsFaulted)
            {
                Exception ex = t.Exception;
                while (ex is AggregateException && ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }

                if (ex is TimeoutException)
                {
                    return true;
                }
            }
            return false;
        }
        

        public static bool IsTaskTimedOutAndStillNeedRetry(Task t, int retryCount)
        {
            return IsTaskTimedOut(t) && retryCount<Constants.max_retries;
        }
    }
}
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

    public static class StringExtensionMethods
    {
        public static int GetStableHashCode(this string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for(int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i+1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i+1];
                }

                return hash1 + (hash2*1566083941);
            }
        }
    }

    public static class ArrayExtension
    {
        public static T[] RemoveAt<T>(this T[] source, int index)
        {
            T[] dest = new T[source.Length - 1];
            if( index > 0 )
                Array.Copy(source, 0, dest, 0, index);

            if( index < source.Length - 1 )
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        }
    }






    static public class Utils
    {
        static public IOrderingEnforcer GetOrderingEnforcerInstance()
        {
            return new OrderingGrainWithSequenceNumber();
            // return new OrderingGrainWithContinuousSending();
        }

        public static readonly string[] OperatorTypes=new string[]{"Scan","Sort","SentimentAnalysis","Filter","GroupBy","HashJoin","HashRippleJoin","CrossRippleJoin","Count","KeywordSearch","Projection"}; 

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


        public static bool IsTaskFaultedAndStillNeedRetry(Task t, int retryCount)
        {
            return t.IsFaulted && retryCount<Constants.max_retries;
        }


        public static string GetOperatorTypeFromGrainClass(string grainClass)
        {
            foreach(string op in OperatorTypes)
            {
                if(grainClass.Contains(op))
                {
                    return op;
                }
            }
            Console.WriteLine("Unknown Operator Found! Make sure to register it in Utils.cs");
            return "Unknown";
        }

        public static string GetReadableName(IGrain grain)
        {
            string ext1,opType1;
            string guidPrefix=grain.GetPrimaryKey(out ext1).ToString().Substring(0,8);
            opType1=Utils.GetOperatorTypeFromGrainClass(grain.GetType().Name);
            return opType1+" "+guidPrefix+" "+ext1;
        }
    }
}
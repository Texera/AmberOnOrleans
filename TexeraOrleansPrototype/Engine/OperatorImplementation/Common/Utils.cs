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

    //https://loune.net/2017/06/running-shell-bash-commands-in-net-core/
    public static class ShellHelper
    {
        public static string Bash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }

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

        public static T[] FastConcat<T>(this T[] source1, T[] source2)
        {
            T[] result = new T[source1.Length + source2.Length];
            Array.Copy(source1, result, source1.Length);
            Array.Copy(source2, 0, result, source1.Length, source2.Length);
            return result;
        }
    }


    public static class StringBuilderExtension
    {
        public static StringBuilder TrimEnd(this StringBuilder sb)
        {
            if (sb == null || sb.Length == 0) return sb;

            int i = sb.Length - 1;
            for (; i >= 0; i--)
                if (!char.IsWhiteSpace(sb[i]))
                    break;

            if (i < sb.Length - 1)
                sb.Length = i + 1;

            return sb;
        }
    }






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

        static public List<string> ListFileNameFromHDFSDirectory(string dir)
        {
            StringBuilder sb=new StringBuilder();
            sb.Append(dir);
            sb.Append("?op=LISTSTATUS");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sb.ToString());
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader Reader = new StreamReader(response.GetResponseStream()))
            {
                string str_response=Reader.ReadToEnd();
                JObject obj =JObject.Parse(str_response);
                List<string> res = new List<string>();
                foreach(JObject o in (JArray)obj["FileStatuses"]["FileStatus"])
                {
                    if((string)o["type"]=="FILE")
                    {
                        res.Add((string)o["pathSuffix"]);
                    }
                }
                return res;
            }
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
            return IsTaskTimedOut(t) && retryCount<Constants.MaxRetries;
        }


        public static bool IsTaskFaultedAndStillNeedRetry(Task t, int retryCount)
        {
            return t.IsFaulted && retryCount<Constants.MaxRetries;
        }

        public static string GetReadableName(IGrain grain)
        {
            string ext1;
            string guidPrefix=grain.GetPrimaryKey(out ext1).ToString().Substring(0,8);
            return guidPrefix+" "+ext1;
        }

    }
}
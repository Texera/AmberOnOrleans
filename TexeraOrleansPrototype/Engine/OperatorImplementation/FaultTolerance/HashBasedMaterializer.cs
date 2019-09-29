using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using Engine.OperatorImplementation.MessagingSemantics;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;
using Orleans.Placement;
using Orleans.Runtime;
using System.Linq;
using Serialize.Linq.Serializers;
using System.Linq.Expressions;

namespace Engine.OperatorImplementation.FaultTolerance
{
    public class HashBasedMaterializer : ITupleProcessor
    {
        private StreamWriter[] sws;
        private int idx;
        private Guid id;
        private int numBuckets;
        private string serializedHashFunc;
        private Func<TexeraTuple,int> hashFunc = null;

        public HashBasedMaterializer(Guid id, int idx,int numBuckets, string hashFunc)
        {
            this.idx = idx;
            this.id = id;
            this.numBuckets = numBuckets;
            this.serializedHashFunc = hashFunc;
        }

        private int NonNegativeModular(int x, int m) {
            return (x%m + m)%m;
        }


        public void Accept(TexeraTuple tuple)
        {
           sws[NonNegativeModular(hashFunc(tuple),numBuckets)].WriteLine(String.Join("|",tuple.FieldList));
        }

        public void OnRegisterSource(Guid from)
        {
            return;
        }

        public void NoMore()
        {
            string currentDir = Environment.CurrentDirectory;
            for(int i=0;i<numBuckets;++i)
            {
                sws[i].Close();
                string pathName = currentDir+"/"+id+"/"+idx+"_"+i+".tmp";
                //put local file to HDFS
                string dirName = "/amber-tmp/"+id+"/"+i;
                ("chmod 777 "+pathName).Bash();
                ("hdfs dfs -put "+pathName+" "+dirName).Bash();
            }
            return;
        }

        public Task Initialize()
        {
            string currentDir = Environment.CurrentDirectory;
            sws = new StreamWriter[numBuckets];
            if (!Directory.Exists(currentDir+"/"+id.ToString()))
            {
                Directory.CreateDirectory(currentDir+"/"+id.ToString());
            }
            for(int i=0;i<numBuckets;++i)
            {
                string pathName = currentDir+"/"+id+"/"+idx+"_"+i+".tmp";
                sws[i] = new StreamWriter(pathName, false, new UTF8Encoding(false), 65536);
            }
            var serializer = new ExpressionSerializer(new JsonSerializer());
            var actualExpression = serializer.DeserializeText(serializedHashFunc);
            hashFunc=((Expression<Func<TexeraTuple,int>>)actualExpression).Compile();
            return Task.CompletedTask;
        }

        public bool HasNext()
        {
            return false;
        }

        public TexeraTuple Next()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            for(int i=0;i<numBuckets;++i)
            {
                sws[i].Close();
            }
        }

        public Task<TexeraTuple> NextAsync()
        {
            throw new NotImplementedException();
        }

        public void MarkSourceCompleted(Guid source)
        {
            
        }
    }
}
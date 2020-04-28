using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;
using Orleans.Runtime;
using System.Linq;

namespace Engine.OperatorImplementation.FaultTolerance
{
    public class LocalFileScanProducer : ITupleProducer
    {
        private ScanStreamReader reader;
        private char separator;
        private string file;

        public LocalFileScanProducer(string file,char separator)
        {
            this.file = file;
            this.separator = separator;
        }

        public async Task Initialize()
        {
            string currentDir = Environment.CurrentDirectory;
            Console.WriteLine("Getting: "+currentDir+"/"+file);
            reader = new ScanStreamReader(currentDir+"/"+file,separator);
            if(!reader.GetFile(0))
                throw new Exception("unable to get file");
            await reader.FillBuffer();
        }

        public bool HasNext()
        {
            return !reader.IsEOF();
        }

        public TexeraTuple Next()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            reader.Close();
        }

        public async Task<TexeraTuple> NextAsync()
        {
            Pair<TexeraTuple,ulong> res=await reader.ReadTuple();
            return res.First;
        }
    }
}
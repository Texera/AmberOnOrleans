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
    public class HashBasedFolderScanProducer : ITupleProducer
    {
        private ScanStreamReader reader;
        private string folder;
        private char separator;
        private Queue<string> fileNames;

        public HashBasedFolderScanProducer(string folder,char separator)
        {
            this.folder = folder;
            this.separator = separator;
        }

        public async Task Initialize()
        {
            if(folder.StartsWith("http://"))
            {
                fileNames = new Queue<string>(Common.Utils.ListFileNameFromHDFSDirectory(folder));
            }
            else
            {
                fileNames = new Queue<string>(Directory.GetFiles(folder, "*",SearchOption.TopDirectoryOnly));
            }
            reader = new ScanStreamReader(folder+"/"+fileNames.Dequeue(),separator);
            if(!reader.GetFile(0))
                throw new Exception("unable to get file");
            await reader.FillBuffer();
        }

        public bool HasNext()
        {
            return !reader.IsEOF() || fileNames.Count != 0;
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
            if(reader.IsEOF() && fileNames.Count > 0)
            {
                reader.Close();
                reader=new ScanStreamReader(folder+"/"+fileNames.Dequeue(),separator);
                if(!reader.GetFile(0))
                    throw new Exception("unable to get file");
                await reader.FillBuffer();
            }
            Pair<TexeraTuple,ulong> res=await reader.ReadTuple();
            return res.First;
        }
    }
}
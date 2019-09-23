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

namespace Engine.OperatorImplementation.Operators
{
    public class ScanProducer : ITupleProducer
    {
        private ulong original_start=0;
        private ulong start,end;
        private ScanStreamReader reader;
        private string file;
        private char separator;

        public ScanProducer(ulong start_byte, ulong end_byte,string file,char separator)
        {
            start=start_byte;
            end=end_byte;
            original_start=start;
            this.file = file;
            this.separator = separator;
        }

        public async Task Initialize()
        {
            reader=new ScanStreamReader(file,separator);
            if(!reader.GetFile(start))
                throw new Exception("unable to get file");
            if(start!=0)
                start+=await reader.TrySkipFirst();
            else
                await reader.FillBuffer();
        }

        public bool HasNext()
        {
            return start<=end && !reader.IsEOF();
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
            start+=res.Second;
            return res.First;
        }
    }
}
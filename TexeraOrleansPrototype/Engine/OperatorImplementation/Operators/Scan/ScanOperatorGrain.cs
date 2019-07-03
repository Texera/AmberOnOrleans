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
    public class ScanOperatorGrain : WorkerGrain, IScanOperatorGrain
    {
        private ulong size=0,original_start=0;
        private ulong start,end;
        private ScanStreamReader reader;
        public int GenerateLimit=Constants.BatchSize*2;
        protected override void Start()
        {
            base.Start();
        }

        protected override void Resume()
        {
            base.Resume();
            if(!isFinished)
            {
                Task.Run(()=>Generate());
            }
        }

        protected override async Task<List<TexeraTuple>> GenerateTuples()
        {
            List<TexeraTuple> outputList=new List<TexeraTuple>();
            for(int i=0;i<GenerateLimit;++i)
            {
                Pair<TexeraTuple,ulong> res=await reader.ReadTuple();
                start+=res.Second;
                if(res.First.FieldList!=null)
                    outputList.Add(res.First);
                if(isPaused)
                {
                    return outputList;
                }
                if(start>end || reader.IsEOF())
                {
                    reader.Close();
                    currentEndFlagCount=0;
                    return outputList;
                }
            }
            return outputList;
        }

        

        public async override Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            SiloAddress addr=await base.Init(self,predicate,principalGrain);
            ulong filesize=((ScanPredicate)predicate).FileSize;
            string extensionKey = "";
            Guid key = this.GetPrimaryKey(out extensionKey);
            ulong i=UInt64.Parse(extensionKey);
            ulong num_grains=(ulong)((ScanPredicate)predicate).NumberOfGrains;
            ulong partition=filesize/num_grains;
            ulong start_byte=i*partition;
            ulong end_byte=num_grains-1==i?filesize:(i+1)*partition;
            reader=new ScanStreamReader(((ScanPredicate)predicate).File,((ScanPredicate)predicate).Separator);
            if(!reader.GetFile(start_byte))
                throw new Exception("unable to get file");
            start=start_byte;
            end=end_byte;
            size=partition;
            original_start=start;
            if(start!=0)
                start+=await reader.TrySkipFirst();
            //Console.WriteLine("Init: start byte: "+start.ToString()+" end byte: "+end.ToString());
            return addr;
        }   
    }
}
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
        private bool restarted=false;
        private double checkpoint=0.1;
        private ulong size=0,original_start=0;
        private ulong start,end,tuple_counter=0;
        private ScanStreamReader reader;
        public static int GenerateLimit=1000;
        // TimeSpan splitingTime=new TimeSpan(0,0,0);
        // TimeSpan addingToListTime=new TimeSpan(0,0,0);
        // TimeSpan generateTime=new TimeSpan(0,0,0);
        // TimeSpan readtupleTime=new TimeSpan(0,0,0);

        protected override void Start()
        {
            base.Start();
        }

        protected override void Resume()
        {
            restarted=true;
            base.Resume();
            if(!isFinished)
            {
                Task.Run(()=>Generate());
            }
        }

        protected override async Task<List<TexeraTuple>> GenerateTuples()
        {
            //DateTime start1=DateTime.UtcNow;
            List<TexeraTuple> outputList=new List<TexeraTuple>();
            for(int i=0;i<GenerateLimit;++i)
            {
                Pair<TexeraTuple,ulong> res=await reader.ReadTuple();
                start+=res.Second;
                //DateTime start2=DateTime.UtcNow;
                if(res.First.FieldList!=null)
                    outputList.Add(res.First);
                if(isPaused)
                {
                    return outputList;
                }
                if(start>end || reader.IsEOF())
                {
                    reader.Close();
                    //Console.WriteLine(Common.Utils.GetReadableName(self)+" Spliting Time: "+splitingTime +" Adding to list Time: "+addingToListTime+" Generate Time: "+generateTime+" ReadTuple Time: "+readtupleTime);
                    //reader.PrintTimeUsage(Common.Utils.GetReadableName(self));
                    currentEndFlagCount=0;
                    return outputList;
                }
                //addingToListTime+=DateTime.UtcNow-start2;
            }
            //generateTime+=DateTime.UtcNow-start1;
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
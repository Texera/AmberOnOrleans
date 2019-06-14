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
        private string separator;

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

        protected async override Task<List<TexeraTuple>> GenerateTuples()
        {
            List<TexeraTuple> outputList=new List<TexeraTuple>();
            for(int i=0;i<GenerateLimit;++i)
            {
                if(isPaused)
                {
                    return outputList;
                }
                if(start>end || reader.IsEOF())
                {
                    reader.PrintTimeUsage();
                    currentEndFlagCount=0;
                    return outputList;
                }
                TexeraTuple tuple=await ReadTuple();
                if(tuple!=null)
                {
                    outputList.Add(tuple);
                }
            }
            return outputList;
        }

        

        public async override Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            SiloAddress addr=await base.Init(self,predicate,principalGrain);
            ulong filesize=((ScanPredicate)predicate).FileSize;
            separator=((ScanPredicate)predicate).Separator;
            string extensionKey = "";
            Guid key = this.GetPrimaryKey(out extensionKey);
            ulong i=UInt64.Parse(extensionKey);
            ulong num_grains=(ulong)((ScanPredicate)predicate).NumberOfGrains;
            ulong partition=filesize/num_grains;
            ulong start_byte=i*partition;
            ulong end_byte=num_grains-1==i?filesize:(i+1)*partition;
            reader=new ScanStreamReader(((ScanPredicate)predicate).File);
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

        


        private async Task<TexeraTuple> ReadTuple()
        {
            try
            {
                Tuple<string,ulong> res = await reader.ReadLine();
                start += res.Item2;
                if (reader.IsEOF())
                {
                    start = end + 1;
                    return null;
                }
                try
                {
                    ++tuple_counter;
                    if(separator!=null)
                        return new TexeraTuple(res.Item1.Split(separator));
                    else
                        return new TexeraTuple(new string[]{res.Item1});
                }
                catch
                {
                    Console.WriteLine("Failed to parse the tuple");
                    return null;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("EXCEPTION: in Reading Tuples from File - "+ ex.ToString());
                Console.WriteLine("start_offset: "+start.ToString()+" end_offset: "+end.ToString());
                int retry=0;
                while(retry<10 && !reader.GetFile(start))
                {
                    retry++;
                    Console.WriteLine("Cannot recover file on {retry} trial, will try again in 5 seconds");
                    Thread.Sleep(5000);
                };
                return null;
            }
        }
        
    }
}
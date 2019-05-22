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
            StartGenerate(0);
        }

        protected override void Resume()
        {
            restarted=true;
            base.Resume();
            if(!isFinished)
            {
                StartGenerate(0);
            }
        }

        protected async override Task GenerateTuples()
        {
            if(restarted)
            {
                Console.WriteLine(Common.Utils.GetReadableName(self)+" restarted scanning file");
                restarted=false;
            }
            if(((start-original_start)/(double)size)>checkpoint)
            {
                Console.WriteLine(Common.Utils.GetReadableName(self)+" reached checkpoint of "+checkpoint);
                checkpoint+=0.1;
            }
            int i=0;
            while(i<GenerateLimit)
            {
                if(start>end)
                {
                    Console.WriteLine(Common.Utils.GetReadableName(self)+" set currentEndFlagCount=0 actionQueue.Count="+actionQueue.Count);
                    currentEndFlagCount=0;
                    break;
                }
                TexeraTuple tuple=await ReadTuple();
                if(tuple!=null)
                {
                    outputTuples.Add(tuple);
                    i++;
                }
                if(reader.IsEOF())
                {
                    Console.WriteLine(Common.Utils.GetReadableName(self)+" set currentEndFlagCount=0 actionQueue.Count="+actionQueue.Count);
                    currentEndFlagCount=0;
                    break;
                }
            }
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
                    return new TexeraTuple(res.Item1.Split(separator));
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
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

namespace Engine.OperatorImplementation.Operators
{
    public class ScanOperatorGrain : NormalGrain, IScanOperatorGrain
    {
        private ulong start,end,seq_number=0,tuple_counter=0;
        private ScanStreamReader reader;
        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }

        public override async Task PauseGrain()
        {
            pause=true;
            await nextGrain.PauseGrain();
        }

        public override async Task ResumeGrain()
        {
            pause=false;
            await nextGrain.ResumeGrain();
            if(start<=end)
            {
                string extensionKey = "";
                Guid primaryKey=this.GetPrimaryKey(out extensionKey);
                IScanOperatorGrain self=GrainFactory.GetGrain<IScanOperatorGrain>(primaryKey,extensionKey);
                await TrySubmitTuples(0,self);
            }
        }


        private async Task TrySubmitTuples(int retryCount, IScanOperatorGrain self)
        {
            self.SubmitTuples().ContinueWith((t) =>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    TrySubmitTuples(retryCount+1, self);
            });
        }

        private async Task TrySendOneBatch(Immutable<List<TexeraTuple>> batch,int retryCount)
        {
            nextGrain.ReceiveTuples(batch,nextGrain).ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                    TrySendOneBatch(batch,retryCount+1);
            });
        } 



        public async Task SubmitTuples() 
        {
            String exKey = "";
            Console.WriteLine($"Start method received by scan grain {this.GetPrimaryKey(out exKey)} - {exKey}");
            if(pause)
            {
                return;
            }
            try
            {
                List<TexeraTuple> batch = new List<TexeraTuple>();
                for (int i = 0; i <Constants.batchSize;)
                {
                    if(start>end)break;
                    TexeraTuple tx;
                    if(ReadTuple(out tx))
                    {
                        batch.Add(tx);
                        ++i;
                    }
                }

                // send this batch
                if(batch.Count>0)
                {
                    Console.WriteLine("Scan" + " sending "+seq_number.ToString());
                    batch[0].seq_token=seq_number++;
                    await TrySendOneBatch(batch.AsImmutable(),0);
                    batch = new List<TexeraTuple>();
                }

                // Grain sends a message to itself to send the next batch
                if(start <= end)
                {
                    string extensionKey = "";
                    Guid primaryKey=this.GetPrimaryKey(out extensionKey);
                    IScanOperatorGrain self=GrainFactory.GetGrain<IScanOperatorGrain>(primaryKey,extensionKey);
                    await TrySubmitTuples(0,self);
                }
                else
                {
                    reader.Close();
                    string extensionKey = "";
                    Console.WriteLine("Scan " + (this.GetPrimaryKey(out extensionKey)).ToString() +" "+extensionKey + " sending done");
                    Console.WriteLine("current offset: "+start.ToString()+" end offset: "+end.ToString());
                    Console.WriteLine("tuple count: "+tuple_counter.ToString());
                    batch.Add(new TexeraTuple(seq_number ,-1, null));
                    await TrySendOneBatch(batch.AsImmutable(),0);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("EXCEPTION in Sending Tuples - "+ ex.ToString());
            }
        }
        
        
        
        public override Task Init()
        {
            ulong filesize=((ScanPredicate)predicate).GetFileSize();
            string extensionKey = "";
            Guid key = this.GetPrimaryKey(out extensionKey);
            ulong i=UInt64.Parse(extensionKey);
            ulong num_grains=(ulong)((ScanPredicate)predicate).GetNumberOfGrains();
            ulong partition=filesize/num_grains;
            ulong start_byte=i*partition;
            ulong end_byte=num_grains-1==i?filesize:(i+1)*partition;
            reader=new ScanStreamReader(((ScanPredicate)predicate).GetFileName());
            if(!reader.GetFile(start_byte))
                return Task.FromException(new Exception("unable to get file"));
            start=start_byte;
            end=end_byte;
            if(start!=0)
                start+=reader.TrySkipFirst();
            Console.WriteLine("Init: start byte: "+start.ToString()+" end byte: "+end.ToString());
            return Task.CompletedTask;
        }

        


        private bool ReadTuple(out TexeraTuple tx)
        {
            try
            {
                ulong ByteCount;
                string res = reader.ReadLine(out ByteCount);
                //Console.WriteLine(res);
                start += ByteCount;
                //Console.WriteLine("offset: "+start+" length: "+res.Length);
                if (reader.IsEOF())
                {
                    start = end + 1;
                    tx=null;
                    return false;
                }
                try
                {
                    tx=new TexeraTuple(tuple_counter, (int)tuple_counter, res.Split(","));
                    ++tuple_counter;
                    return true;
                }
                catch
                {
                    tx=null;
                    Console.WriteLine("Failed to parse the tuple");
                    return false;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("EXCEPTION in Reading Tuples from File - "+ ex.ToString());
                Console.WriteLine("start_offset: "+start.ToString()+" end_offset: "+end.ToString());
                if(!reader.GetFile(start))throw new Exception("Reading Tuple: Cannot Get File");
                tx=null;
                return false;
            }
        }
        
    }
}
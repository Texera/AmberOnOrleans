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

        enum FileType{unknown,csv,pdf,txt};
        FileType file_type;
        System.IO.StreamReader file;
        string file_path;
        ulong start,end,seq_number=0,tuple_counter=0;
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
                await MakeSubmitTuples(0);
            }
        }


        public async Task MakeSubmitTuples(int retryCount)
        {
            string extensionKey = "";
            Guid primaryKey=this.GetPrimaryKey(out extensionKey);
            IScanOperatorGrain self=GrainFactory.GetGrain<IScanOperatorGrain>(primaryKey,extensionKey);
            self.SubmitTuples().ContinueWith((t) =>
            {
                if(Utils.IsTaskTimedout(t) && retryCount<Constants.max_retries)
                    MakeSubmitTuples(retryCount+1);
            });
        }


        public async Task SubmitTuples() 
        {
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
                    // string extensionKey = "";
                    // Guid primaryKey=this.GetPrimaryKey(out extensionKey);
                    //Console.WriteLine("Scan " + (this.GetPrimaryKey(out extensionKey)).ToString() +" "+extensionKey + " sending "+seq_number.ToString());
                    batch[0].seq_token=seq_number++;
                    await (nextGrain).ReceiveTuples(batch.AsImmutable(),nextGrain);
                    batch = new List<TexeraTuple>();
                }

                // Grain sends a message to itself to send the next batch
                if(start <= end)
                {
                    string extensionKey = "";
                    Guid primaryKey=this.GetPrimaryKey(out extensionKey);
                    IScanOperatorGrain self=GrainFactory.GetGrain<IScanOperatorGrain>(primaryKey,extensionKey);
                    await self.MakeSubmitTuples(0);
                }
                else
                {
                    file.Close();
                    string extensionKey = "";
                    Console.WriteLine("Scan " + (this.GetPrimaryKey(out extensionKey)).ToString() +" "+extensionKey + " sending done");
                    Console.WriteLine("current offset: "+start.ToString()+" end offset: "+end.ToString());
                    Console.WriteLine("tuple count: "+tuple_counter.ToString());
                    batch.Add(new TexeraTuple(seq_number ,-1, null));
                    await (nextGrain).ReceiveTuples(batch.AsImmutable(),nextGrain);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("EXCEPTION in Sending Tuples - "+ ex.ToString());
            }
        }
        
        private void TrySkipFirst()
        {
            switch(file_type)
            {
                case FileType.csv:
                if (start != 0)
                {
                    string res=file.ReadLine();
                    Console.WriteLine("Skip: "+res);
                    this.start += (ulong)(Encoding.UTF8.GetByteCount(res)+Environment.NewLine.Length);
                }
                break;
                default:
                //not implemented
                break;
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
            file_path = ((ScanPredicate)predicate).GetFileName();
            if(!GetFile(file_path,start_byte))
                return Task.FromException(new Exception("unable to get file"));
            start=start_byte;
            end=end_byte;
            if(Enum.TryParse<FileType>(file_path.Substring(file_path.LastIndexOf(".")+1),out file_type))
            {
                if(!Enum.IsDefined(typeof(FileType),file_type))
                    file_type=FileType.unknown;
            }
            else
                file_type=FileType.unknown;
            TrySkipFirst();
            Console.WriteLine("Init: "+file_type.ToString()+" start byte: "+start.ToString()+" end byte: "+end.ToString());
            return Task.CompletedTask;
        }

        private bool GetFile(string path, ulong offset)
        {
            try
            {
                if(path.StartsWith("http://"))
                {
                    //HDFS RESTful read
                    string uri_path=Utils.GenerateURLForHDFSWebAPI(path,offset);
                    file=Utils.GetFileHandleFromHDFS(uri_path);
                }
                else
                {
                    //normal read
                    file = new System.IO.StreamReader(file_path);
                    if(file.BaseStream.CanSeek)
                        file.BaseStream.Seek((long)offset,SeekOrigin.Begin);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("EXCEPTION in Opening File - "+ ex.ToString());
                return false;
            }
            return true;
        }


        private bool ReadTuple(out TexeraTuple tx)
        {
            try
            {
                string res = file.ReadLine();
                start += (ulong)(Encoding.UTF8.GetByteCount(res)+Environment.NewLine.Length);
                //Console.WriteLine("offset: "+start+" length: "+res.Length+" "+extensionKey+" "+res);
                if (file.EndOfStream)
                    start = end + 1;
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
                GetFile(file_path,start);
                tx=null;
                return false;
            }
        }


    }
}
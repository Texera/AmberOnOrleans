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
            await nextGrain.PauseGrain();
        }

        public override async Task ResumeGrain()
        {
            await nextGrain.ResumeGrain();
        }

        public async Task SubmitTuples() 
        {
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
                if(batch.Count>0)
                {
                    batch[0].seq_token=seq_number++;
                    await (nextGrain).ReceiveTuples(batch.AsImmutable(),nextGrain);
                    batch = new List<TexeraTuple>();
                }
                if(start <= end)
                {
                    string extensionKey = "";
                    Guid primaryKey=this.GetPrimaryKey(out extensionKey);
                    IScanOperatorGrain self=GrainFactory.GetGrain<IScanOperatorGrain>(primaryKey,extensionKey);
                    self.SubmitTuples();
                }
                else
                {
                    file.Close();
                    string extensionKey = "";
                    Console.WriteLine("Scan " + (this.GetPrimaryKey(out extensionKey)).ToString() +" "+extensionKey + " sending done "+seq_number.ToString());
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
                    this.start += (ulong)file.ReadLine().Length;
                break;
                default:
                //not implemented
                break;
            }
        }
        
        public Task Init(ulong start_byte,ulong end_byte)
        {
            if(!GetFile(((ScanPredicate)predicate).GetFileName(),start_byte))
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
            return Task.CompletedTask;
        }

        private bool GetFile(string path, ulong offset)
        {
            try
            {
                if(path.StartsWith("http://"))
                {
                    //HDFS RESTful read
                    file_path=Utils.GenerateURLForHDFSWebAPI(file_path,offset);
                    file=Utils.GetFileHandleFromHDFS(file_path);
                }
                else
                {
                    //normal read
                    file_path = path;
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
                start += (ulong)res.Length;
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
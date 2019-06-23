using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;

class ScanStreamReader
{
    public const int buffer_size = 1024;
    private enum FileType{unknown,csv,pdf,txt,tbl};
    private FileType file_type;
    private string file_path;
    private System.IO.StreamReader file=null;
    private byte[] buffer = new byte[buffer_size];
    private int buffer_start = 0;
    private int buffer_end = 0;
    StringBuilder sb=new StringBuilder();
    char[] charbuf=new char[buffer_size];
    // private TimeSpan reading=new TimeSpan(0,0,0);
    // private TimeSpan forloop=new TimeSpan(0,0,0);
    // private TimeSpan generate=new TimeSpan(0,0,0);
    private Decoder decoder;
    private List<String> fields=new List<string>();
    private char delimiter;

    public ScanStreamReader(string path,char delimiter)
    {
        file_path=path;
        this.delimiter=delimiter;
    }


    public async Task<ulong> TrySkipFirst()
    {
        if(file==null)throw new Exception("TrySkipFirst: File Not Exists");
        switch(file_type)
        {
            case FileType.csv:
            case FileType.tbl:
            case FileType.txt:
            Pair<TexeraTuple,ulong> res=await ReadTuple();
            //Console.WriteLine("Skip: "+res);
            return res.Second;
            default:
            //not implemented
            break;
        }
        return 0;
    }

    public bool GetFile(ulong offset)
    {
        try
        {
            if(file_path.StartsWith("http://"))
            {
                //HDFS RESTful read
                //example path to HDFS through WebHDFS API: "http://localhost:50070/webhdfs/v1/input/very_large_input.csv"
                string uri_path=Utils.GenerateURLForHDFSWebAPI(file_path,offset);
                file=Utils.GetFileHandleFromHDFS(uri_path);
            }
            else
            {
                //normal read
                file = new System.IO.StreamReader(file_path);
                if(file.BaseStream.CanSeek)
                    file.BaseStream.Seek((long)offset,SeekOrigin.Begin);
            }
            decoder=file.CurrentEncoding.GetDecoder();
            if(Enum.TryParse<FileType>(file_path.Substring(file_path.LastIndexOf(".")+1),out file_type))
            {
                if(!Enum.IsDefined(typeof(FileType),file_type))
                    file_type=FileType.unknown;
            }
            else
                file_type=FileType.unknown;
        }
        catch(Exception ex)
        {
            Console.WriteLine("EXCEPTION in Opening File - "+ ex.ToString());
            return false;
        }
        return true;
    }

    public async Task<Pair<TexeraTuple,ulong>> ReadTuple()
    {
        if(file==null)throw new Exception("ReadLine: File Not Exists");
        //DateTime start=DateTime.UtcNow;
        sb.Length=0;
        fields.Clear();
        ulong ByteCount=0;
        while(true)
        {
            if(buffer_start>=buffer_end)
            {
                buffer_start=0;
                //DateTime start1=DateTime.UtcNow;
                try
                {
                    buffer_end=await file.BaseStream.ReadAsync(buffer,0,buffer_size);    
                }
                catch(Exception e)
                {
                    buffer_end=0;
                    throw e;
                }
                //reading+=DateTime.UtcNow-start1; 
            }
            if(buffer_end==0)break;
            int i;
            int charbuf_length;
            //start=DateTime.UtcNow;
            for(i=buffer_start;i<buffer_end;++i)
            {
                if(buffer[i]==delimiter)
                {
                    int length=i-buffer_start;
                    ByteCount+=(ulong)(length+1);
                    charbuf_length=decoder.GetChars(buffer,buffer_start,length,charbuf,0);
                    sb.Append(charbuf,0,charbuf_length);
                    fields.Add(sb.ToString());
                    sb.Length=0;
                    buffer_start=i+1;
                }
                else if(buffer[i]=='\n')
                {
                    int length=i-buffer_start;
                    ByteCount+=(ulong)(length+1);
                    charbuf_length=decoder.GetChars(buffer,buffer_start,length,charbuf,0);
                    fields.Add(sb.ToString());
                    buffer_start=i+1;
                    sb.Length=0;
                    //forloop+=DateTime.UtcNow-start;
                    //DateTime start2=DateTime.UtcNow;
                    return new Pair<TexeraTuple,ulong>(new TexeraTuple(fields.ToArray()),ByteCount);
                    //generate+=DateTime.UtcNow-start2;
                    //return v;
                }
            }
            ByteCount+=(ulong)(buffer_end-buffer_start);
            charbuf_length=decoder.GetChars(buffer,buffer_start,buffer_end-buffer_start,charbuf,0);
            sb.Append(charbuf,0,charbuf_length);
            buffer_start=buffer_end;
        }
        //forloop+=DateTime.UtcNow-start;
        if(fields.Count>0)
            return new Pair<TexeraTuple,ulong>(new TexeraTuple(fields.ToArray()),ByteCount);
        else
            return new Pair<TexeraTuple,ulong>(new TexeraTuple(null),ByteCount);
    }
    public void Close()
    {
        if(file!=null)
        {
            file.Close();
            file=null;
        }
    }

    public bool IsEOF()
    {
        return buffer_end==0;
        // return file.EndOfStream;
    }

    // public void PrintTimeUsage(string grain)
    // {
    //     Console.WriteLine(grain+" Reading from HDFS to buffer Time: "+reading);
    //     Console.WriteLine(grain+" Reading from buffer Time: "+forloop);
    //     Console.WriteLine(grain+" Generate Time: "+generate);
    // }


}
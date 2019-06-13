using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Engine.OperatorImplementation.Common;

class ScanStreamReader
{
    public const int buffer_size = 4*1024;
    private enum FileType{unknown,csv,pdf,txt,tbl};
    private FileType file_type;
    private string file_path;
    private System.IO.StreamReader file=null;
    private byte[] buffer = new byte[buffer_size];
    private int buffer_start = 0;
    private int buffer_end = 0;
    private Decoder decoder;

    public ScanStreamReader(string path)
    {
        file_path=path;
    }


    public async Task<ulong> TrySkipFirst()
    {
        if(file==null)throw new Exception("TrySkipFirst: File Not Exists");
        switch(file_type)
        {
            case FileType.csv:
            case FileType.tbl:
            case FileType.txt:
            Tuple<string,ulong> res=await ReadLine();
            //Console.WriteLine("Skip: "+res);
            return res.Item2;
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

    public async Task<Tuple<string,ulong>> ReadLine()
    {
        if(file==null)throw new Exception("ReadLine: File Not Exists");
        ulong ByteCount=0;
        StringBuilder sb=new StringBuilder();
        char[] charbuf=new char[buffer_size];
        while(true)
        {
            if(buffer_start>=buffer_end)
            {
                buffer_start=0;
                try
                {
                    await Task.Delay(50);//buffer_end=await file.BaseStream.ReadAsync(buffer,0,buffer_size);
                }
                catch(Exception e)
                {
                    buffer_end=0;
                    throw e;
                }
            }
            buffer_end=-1;
            return new Tuple<string,ulong>("1|1|3|4|5|6|7|8|9",40);
            if(buffer_end==0)break;
            int i;
            int charbuf_length;
            for(i=buffer_start;i<buffer_end;++i)
            {
                if(buffer[i]=='\n')
                {
                    int length=i-buffer_start+1;
                    ByteCount+=(ulong)(length);
                    charbuf_length=decoder.GetChars(buffer,buffer_start,length,charbuf,0);
                    sb.Append(charbuf,0,charbuf_length);
                    buffer_start=i+1;
                    return new Tuple<string,ulong>(sb.ToString().TrimEnd(),ByteCount);
                }
            }
            ByteCount+=(ulong)(buffer_end-buffer_start);
            charbuf_length=decoder.GetChars(buffer,buffer_start,buffer_end-buffer_start,charbuf,0);
            sb.Append(charbuf,0,charbuf_length);
            buffer_start=buffer_end;
        }
        return new Tuple<string,ulong>(sb.ToString().TrimEnd(),ByteCount);
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
    }

}
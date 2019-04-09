using Engine.OperatorImplementation.Common;
using Newtonsoft.Json.Linq;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanPredicate : PredicateBase
    {
        public int NumberOfGrains;
        public string File;
        public ulong FileSize;
        public int TableID;
        public ScanPredicate(string file,int tableID,int outputLimitPerBatch=-1,int batchingLimit=1000,int timeLimitPerBatch=-1):base(outputLimitPerBatch,batchingLimit,timeLimitPerBatch)
        {
            if(file == null)
            {
                File = "";
                FileSize=0;
            }
            else
            {
                File = file;
                if(file.StartsWith("http://"))
                {
                    FileSize=Utils.GetFileLengthFromHDFS(file);
                }
                else
                    FileSize=(ulong)new System.IO.FileInfo(file).Length;
            }
            TableID=tableID;
        }
    }
}
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
        public ScanPredicate(string file,int tableID)
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
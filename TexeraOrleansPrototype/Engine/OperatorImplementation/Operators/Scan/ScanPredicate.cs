using Engine.OperatorImplementation.Common;
using Newtonsoft.Json.Linq;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanPredicate : PredicateBase
    {
        public int NumberOfGrains;
        public string File;
        private ulong fileSize;
        public ulong FileSize 
        {
            get
            {
                if(!filesize_init)
                {
                    if(File.StartsWith("http://"))
                    {
                        fileSize=Utils.GetFileLengthFromHDFS(File);
                    }
                    else
                        fileSize=(ulong)new System.IO.FileInfo(File).Length;
                    return fileSize;
                }
                else
                    return fileSize;
            }
        }
        public string Separator;
        bool filesize_init=false;
        public ScanPredicate(string file,int batchingLimit=1000):base(batchingLimit)
        {
            if(file == null)
            {
                File = "";
                fileSize=0;
                filesize_init=true;
            }
            else
            {
                File = file;
                fileSize=0;
                filesize_init=false;
                
            }
            if(file.EndsWith(".tbl"))
                Separator="|";
            else
                Separator=",";
        }
    }
}
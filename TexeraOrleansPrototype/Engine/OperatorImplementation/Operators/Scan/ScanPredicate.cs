using Engine.OperatorImplementation.Common;
using Newtonsoft.Json.Linq;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanPredicate : PredicateBase
    {
        string file;
        ulong filesize;
        int tableID;
        public ScanPredicate(string file,int tableID)
        {
            if(file == null)
            {
                file = "";
                filesize=0;
            }
            else
            {
                this.file = file;
                if(file.StartsWith("http://"))
                {
                    filesize=Utils.GetFileLengthFromHDFS(file);
                }
                else
                    filesize=(ulong)new System.IO.FileInfo(file).Length;
            }
            this.tableID=tableID;
        }

        public string GetFileName()
        {
            return file;
        }


        public ulong GetFileSize()
        {
            return filesize;
        }
    }
}
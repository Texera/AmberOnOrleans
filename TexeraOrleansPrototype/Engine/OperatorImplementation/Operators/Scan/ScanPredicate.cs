using Engine.OperatorImplementation.Common;
using Newtonsoft.Json.Linq;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanPredicate : PredicateBase
    {
        
        string file;
        ulong filesize;
        int num_grains;
        public ScanPredicate(string file)
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
        }

        public string GetFileName()
        {
            return file;
        }


        public ulong GetFileSize()
        {
            return filesize;
        }

        public int GetNumberOfGrains()
        {
            return num_grains;
        }

        public override Operator GetNewOperator(int numberOfGrains)
        {
            num_grains=numberOfGrains;
            return new ScanOperator(this, numberOfGrains);
        }
    }
}
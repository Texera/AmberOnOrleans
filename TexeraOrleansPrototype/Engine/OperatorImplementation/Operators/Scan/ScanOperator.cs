using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Breakpoint.GlobalBreakpoint;
using Engine.DeploySemantics;
using Engine.LinkSemantics;
using Engine.OperatorImplementation.Common;
using Orleans;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanOperator : Operator
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
        public char Separator;
        bool filesize_init=false;
        private HashSet<int> idxes;
        public ScanOperator(string file,HashSet<int> idxes)
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
                Separator='|';
            else if(file.EndsWith(".csv"))
                Separator=',';
            else 
                Separator='\0';
            this.idxes = idxes;
        }

        public override Pair<List<WorkerLayer>, List<LinkStrategy>> GenerateTopology()
        {
            ulong filesize=FileSize;
            ulong num_grains=(ulong)Constants.DefaultNumGrainsInOneLayer;
            ulong partition=filesize/num_grains;

            return new Pair<List<WorkerLayer>,List<LinkStrategy>>
            (
                new List<WorkerLayer>
                {
                    new ProducerWorkerLayer("scan.main",Constants.DefaultNumGrainsInOneLayer,(i)=>
                    {
                        ulong idx =(ulong)i;
                        ulong start_byte=idx*partition;
                        ulong end_byte=num_grains-1==idx?filesize:(idx+1)*partition;
                        return new ScanProducer(start_byte,end_byte,File,Separator,idxes);
                    },null)
                },
                new List<LinkStrategy>
                {

                }
            );
        }

        public override void AssignBreakpoint(List<WorkerLayer> layers, Dictionary<IWorkerGrain, WorkerState> states, GlobalBreakpointBase breakpoint)
        {
            breakpoint.Partition(layers[0].Layer.Values.SelectMany(x=>x).Where(x => states[x]!=WorkerState.Completed).ToList());
        }

        public override bool IsStaged(Operator from)
        {
            throw new NotImplementedException();
        }

        public override string GetHashFunctionAsString(Guid from)
        {
            throw new NotImplementedException();
        }
    }
}
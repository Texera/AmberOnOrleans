using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Engine.Breakpoint.GlobalBreakpoint;
using Engine.DeploySemantics;
using Engine.LinkSemantics;
using Engine.OperatorImplementation.Common;
using Engine.OperatorImplementation.FaultTolerance;
using Orleans;
using Serialize.Linq.Serializers;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{
    public class HashJoinOperator : Operator
    {
        public int InnerTableIndex;
        public int OuterTableIndex;
        public Guid InnerTableID=Guid.Empty;

        public HashJoinOperator(int innerTableIndex,int outerTableIndex, Guid innerTableID)
        {
            this.InnerTableID = innerTableID;
            this.InnerTableIndex = innerTableIndex;
            this.OuterTableIndex = outerTableIndex;
            Console.WriteLine("InnerTableID: "+innerTableID);
        }

        public override void AssignBreakpoint(List<WorkerLayer> layers, Dictionary<IWorkerGrain, WorkerState> states, GlobalBreakpointBase breakpoint)
        {
            throw new System.NotImplementedException();
        }

        public override Pair<List<WorkerLayer>, List<LinkStrategy>> GenerateTopology()
        {
            return new Pair<List<WorkerLayer>,List<LinkStrategy>>
            (
                new List<WorkerLayer>
                {
                    new ProcessorWorkerLayer("hash_join.main",Constants.DefaultNumGrainsInOneLayer,(i)=>new HashJoinProcessor(InnerTableIndex,OuterTableIndex,InnerTableID),null)
                },
                new List<LinkStrategy>
                {

                }
            );
        }

        public override string GetHashFunctionAsString(Guid from)
        {
            int joinFieldIndex;
            if(from.Equals(InnerTableID))
            {
                Console.WriteLine(from+" use inner idx = "+InnerTableIndex);
                joinFieldIndex=InnerTableIndex;
            }
            else
            {
                Console.WriteLine(from+" use outer idx = "+OuterTableIndex);
                joinFieldIndex=OuterTableIndex;
            }
            Expression<Func<TexeraTuple,int>> exp=tuple=>tuple.FieldList[joinFieldIndex].GetStableHashCode();
            var serializer = new ExpressionSerializer(new JsonSerializer());
            return serializer.SerializeText(exp);
        }

        public override bool IsStaged(Operator from)
        {
            if(from.GetType() == typeof(HashBasedFolderScanOperator))
            {
                return true;
            }
            return false;
        }
    }
}
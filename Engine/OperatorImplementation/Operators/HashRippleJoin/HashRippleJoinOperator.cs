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
    public class HashRippleJoinOperator : Operator
    {
        public int InnerTableIndex;
        public int OuterTableIndex;
        public Guid InnerTableID=Guid.Empty;

        public HashRippleJoinOperator(int innerTableIndex,int outerTableIndex, Guid innerTableID)
        {
            this.InnerTableID = innerTableID;
            this.InnerTableIndex = innerTableIndex;
            this.OuterTableIndex = outerTableIndex;
        }

        public override void AssignBreakpoint(List<WorkerLayer> layers, Dictionary<IWorkerGrain, WorkerState> states, GlobalBreakpointBase breakpoint)
        {
            throw new NotImplementedException();
        }

        public override Pair<List<WorkerLayer>, List<LinkStrategy>> GenerateTopology()
        {
            return new Pair<List<WorkerLayer>,List<LinkStrategy>>
            (
                new List<WorkerLayer>
                {
                    new ProcessorWorkerLayer("hash_ripple_join.main",Constants.DefaultNumGrainsInOneLayer,(i)=>new HashRippleJoinProcessor(InnerTableIndex,OuterTableIndex,InnerTableID),null)
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
                joinFieldIndex=InnerTableIndex;
            else
                joinFieldIndex=OuterTableIndex;
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
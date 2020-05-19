using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using Engine.OperatorImplementation.MessagingSemantics;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;
using System.Linq;
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{
    public class CrossRippleJoinProcessor : ITupleProcessor
    {
        Dictionary<string,List<TexeraTuple>> innerJoinedTuples;
        Dictionary<string,List<TexeraTuple>> outerJoinedTuples;
        int innerTableIndex=-1;
        int outerTableIndex=-1;
        Guid innerTableGuid=Guid.Empty;
        int joinFieldIndex;
        Dictionary<string,List<TexeraTuple>> joinedTuples;
        Dictionary<string,List<TexeraTuple>> toInsert;
        Queue<TexeraTuple> output;

        public CrossRippleJoinProcessor(int innerTableIndex,int outerTableIndex, Guid innerTableGuid)
        {
            this.innerTableIndex = innerTableIndex;
            this.outerTableIndex = outerTableIndex;
            this.innerTableGuid = innerTableGuid;
        }

        public void Accept(TexeraTuple tuple)
        {
            string field=tuple.FieldList[joinFieldIndex];
            List<string> fields=tuple.FieldList.ToList();
            if(joinedTuples.ContainsKey(field))
            {
                foreach(TexeraTuple joinedTuple in joinedTuples[field])
                {
                    output.Enqueue(new TexeraTuple(joinedTuple.FieldList.Concat(fields).ToArray()));
                }
            }
            if(!toInsert.ContainsKey(field))
            {
                toInsert.Add(field,new List<TexeraTuple>{tuple});
            }
            else
            {
                toInsert[field].Add(tuple);
            }
        }

        public void OnRegisterSource(Guid from)
        {
            if(innerTableGuid.Equals(from))
            {
                joinFieldIndex=innerTableIndex;
                joinedTuples=outerJoinedTuples;
                toInsert=innerJoinedTuples;
            }
            else
            {
                joinFieldIndex=outerTableIndex;
                joinedTuples=innerJoinedTuples;
                toInsert=outerJoinedTuples;
            }
        }

        public void NoMore()
        {
            return;
        }

        public Task Initialize()
        {
            innerJoinedTuples=new Dictionary<string, List<TexeraTuple>>();
            outerJoinedTuples=new Dictionary<string, List<TexeraTuple>>();
            output = new Queue<TexeraTuple>();
            return Task.CompletedTask;
        }

        public bool HasNext()
        {
            return output.Count > 0;
        }

        public TexeraTuple Next()
        {
            return output.Dequeue();
        }

        public void Dispose()
        {
            innerJoinedTuples=null;
            outerJoinedTuples=null;
        }

        public Task<TexeraTuple> NextAsync()
        {
            throw new NotImplementedException();
        }

        public void MarkSourceCompleted(Guid source)
        {
            
        }
    }

}
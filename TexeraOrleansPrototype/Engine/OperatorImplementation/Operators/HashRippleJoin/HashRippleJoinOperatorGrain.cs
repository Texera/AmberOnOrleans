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
    public class HashRippleJoinOperatorGrain : WorkerGrain, IHashRippleJoinOperatorGrain
    {
        Dictionary<string,List<TexeraTuple>> innerJoinedTuples=new Dictionary<string, List<TexeraTuple>>();
        Dictionary<string,List<TexeraTuple>> outerJoinedTuples=new Dictionary<string, List<TexeraTuple>>();
        int innerTableIndex=-1;
        int outerTableIndex=-1;
        Guid innerTableGuid=Guid.Empty;

        int joinFieldIndex;
        Dictionary<string,List<TexeraTuple>> joinedTuples;
        Dictionary<string,List<TexeraTuple>> toInsert;

        public override Task OnDeactivateAsync()
        {
            base.OnDeactivateAsync();
            innerJoinedTuples=null;
            outerJoinedTuples=null;
            return Task.CompletedTask;
        }

        public override async Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            SiloAddress addr=await base.Init(self,predicate,principalGrain);
            innerTableIndex=((HashRippleJoinPredicate)predicate).InnerTableIndex;
            outerTableIndex=((HashRippleJoinPredicate)predicate).OuterTableIndex;
            innerTableGuid=((HashRippleJoinPredicate)predicate).InnerTableID;
            return addr;
        }

        protected override void BeforeProcessBatch(PayloadMessage message)
        {
            string ext;
            if(innerTableGuid.Equals(message.SenderIdentifer.GetPrimaryKey(out ext)))
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


        protected override void ProcessTuple(in TexeraTuple tuple,List<TexeraTuple> output)
        {
            string field=tuple.FieldList[joinFieldIndex];
            List<string> fields=tuple.FieldList.ToList();
            fields.RemoveAt(joinFieldIndex);
            if(joinedTuples.ContainsKey(field))
            {
                foreach(TexeraTuple joinedTuple in joinedTuples[field])
                {
                    output.Add(new TexeraTuple(joinedTuple.FieldList.Concat(fields).ToArray()));
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
    }

}
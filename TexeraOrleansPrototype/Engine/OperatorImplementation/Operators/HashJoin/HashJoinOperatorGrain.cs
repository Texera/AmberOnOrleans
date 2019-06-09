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
    public class HashJoinOperatorGrain : WorkerGrain, IHashJoinOperatorGrain
    {
        Dictionary<String,List<string[]>> hashTable=new Dictionary<string, List<string[]>>();
        List<TexeraTuple> otherTable=new List<TexeraTuple>();
        int innerTableIndex=-1;
        int outerTableIndex=-1;
        bool isCurrentInnerTable=false;
        bool isInnerTableFinished=false;
        Guid innerTableGuid=Guid.Empty;

        public override Task OnDeactivateAsync()
        {
            base.OnDeactivateAsync();
            hashTable=null;
            otherTable=null;
            return Task.CompletedTask;
        }

        public override async Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            SiloAddress addr=await base.Init(self,predicate,principalGrain);
            innerTableIndex=((HashJoinPredicate)predicate).InnerTableIndex;
            outerTableIndex=((HashJoinPredicate)predicate).OuterTableIndex;
            innerTableGuid=((HashJoinPredicate)predicate).InnerTableID;
            return addr;
        }


        protected override void BeforeProcessBatch(Immutable<PayloadMessage> message, TaskScheduler orleansScheduler)
        {
            string ext;
            isCurrentInnerTable=innerTableGuid.Equals(message.Value.SenderIdentifer.GetPrimaryKey(out ext));
            isInnerTableFinished=(inputInfo[innerTableGuid]==0);
        }

        protected override void ProcessTuple(in TexeraTuple tuple,List<TexeraTuple> output)
        {
            if(isCurrentInnerTable)
            {
                string source=tuple.FieldList[innerTableIndex];
                if(!hashTable.ContainsKey(source))
                    hashTable[source]=new List<string[]>{tuple.FieldList.RemoveAt(innerTableIndex)};
                else
                    hashTable[source].Add(tuple.FieldList.RemoveAt(innerTableIndex));
            }
            else
            {
                if(!isInnerTableFinished)
                {
                    otherTable.Add(tuple);
                }
                else
                {
                    string field=tuple.FieldList[outerTableIndex];
                    if(hashTable.ContainsKey(field))
                    {
                        foreach(string[] f in hashTable[field])
                        {  
                            output.Add(new TexeraTuple(f.Concat(tuple.FieldList).ToArray()));
                        }
                    }
                }
            }
        }

        protected override void AfterProcessBatch(Immutable<PayloadMessage> message, TaskScheduler orleansScheduler)
        {
            if(inputInfo[innerTableGuid]==0 && otherTable!=null)
            {
                var batch=otherTable;
                Action action=()=>
                {
                    isCurrentInnerTable=false;
                    isInnerTableFinished=true;
                    List<TexeraTuple> outputList=new List<TexeraTuple>();
                    if(batch!=null)
                    {
                        ProcessBatch(batch,outputList);
                    }
                    if(isPaused)
                    {
                        MakePayloadMessagesThenSend(outputList);
                        taskDidPaused=true;
                        return;
                    }
                    batch=null;
                    currentIndex=0;
                    MakePayloadMessagesThenSend(outputList);
                    lock(actionQueue)
                    {
                        actionQueue.Dequeue();
                        if(actionQueue.Count>0)
                        {
                            Task.Run(actionQueue.Peek());
                        }
                    }
                };
                lock(actionQueue)
                {
                    actionQueue.Enqueue(action);
                    if(actionQueue.Count==1)
                    {
                        Task.Run(action);
                    }
                }
                otherTable=null;
            }
        }
    }
}
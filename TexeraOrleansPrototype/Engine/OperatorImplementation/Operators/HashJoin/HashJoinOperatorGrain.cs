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

namespace Engine.OperatorImplementation.Operators
{
    public class HashJoinOperatorGrain : WorkerGrain, IHashJoinOperatorGrain
    {
        Dictionary<String,List<TexeraTuple>> hashTable=new Dictionary<string, List<TexeraTuple>>();
        List<TexeraTuple> otherTable=new List<TexeraTuple>();
        int innerTableIndex=-1;
        int outerTableIndex=-1;
        bool isCurrentInnerTable=false;
        bool isInnerTableFinished=false;
        Guid innerTableGuid=Guid.Empty;
        int TableID;
        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            innerTableIndex=((HashJoinPredicate)predicate).InnerTableIndex;
            outerTableIndex=((HashJoinPredicate)predicate).OuterTableIndex;
            innerTableGuid=((HashJoinPredicate)predicate).InnerTableID;
            TableID=((HashJoinPredicate)predicate).TableID;
            return Task.CompletedTask;
        }


        protected override void BeforeProcessBatch(Immutable<PayloadMessage> message, TaskScheduler orleansScheduler)
        {
            string ext;
            isCurrentInnerTable=innerTableGuid.Equals(message.Value.SenderIdentifer.GetPrimaryKey(out ext));
            isInnerTableFinished=(inputInfo[innerTableGuid]==0);
        }

        protected override void ProcessTuple(TexeraTuple tuple,List<TexeraTuple> output)
        {
            if(isCurrentInnerTable)
            {
                string source=tuple.FieldList[innerTableIndex];
                if(!hashTable.ContainsKey(source))
                    hashTable[source]=new List<TexeraTuple>{tuple};
                else
                    hashTable[source].Add(tuple);
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
                    List<string> fields=tuple.FieldList.ToList();
                    fields.RemoveAt(outerTableIndex);
                    if(hashTable.ContainsKey(field))
                    {
                        foreach(TexeraTuple t in hashTable[field])
                        {  
                            output.Add(new TexeraTuple(TableID,t.FieldList.Concat(fields).ToArray()));
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
                Action action=async ()=>
                {
                    isCurrentInnerTable=false;
                    isInnerTableFinished=true;
                    if(batch!=null)
                    {
                        ProcessBatch(batch);
                    }
                    if(isPaused)
                    {
                        return;
                    }
                    currentIndex=0;
                    await Task.Factory.StartNew(()=>{MakePayloadMessagesThenSend();},CancellationToken.None,TaskCreationOptions.None,orleansScheduler);
                    lock(actionQueue)
                    {
                        actionQueue.Dequeue();
                        if(!isPaused && actionQueue.Count>0)
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
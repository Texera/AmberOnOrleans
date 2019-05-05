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
        int tableSource=-1;
        string sourceOperator=null;
        List<TexeraTuple> otherTable=new List<TexeraTuple>();
        int joinFieldIndex=-1;
        int TableID;
        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            joinFieldIndex=((HashJoinPredicate)predicate).JoinFieldIndex;
            TableID=((HashJoinPredicate)predicate).TableID;
            return Task.CompletedTask;
        }
        protected override void ProcessTuple(TexeraTuple tuple)
        {
            if(tableSource==-1)
            {
                tableSource=tuple.TableID;
            }
            if(tuple.TableID.Equals(tableSource))
            {
                string source=tuple.FieldList[joinFieldIndex];
                if(!hashTable.ContainsKey(source))
                    hashTable[source]=new List<TexeraTuple>{tuple};
                else
                    hashTable[source].Add(tuple);
            }
            else
            {
                if(inputInfo[sourceOperator]!=0)
                {
                    otherTable.Add(tuple);
                }
                else
                {
                    string field=tuple.FieldList[joinFieldIndex];
                    List<string> fields=tuple.FieldList.ToList();
                    fields.RemoveAt(joinFieldIndex);
                    foreach(TexeraTuple t in hashTable[field])
                    {  
                        outputTuples.Enqueue(new TexeraTuple(TableID,t.FieldList.Concat(fields).ToArray()));
                    }
                }
            }
        }

        protected override void AfterProcessBatch(Immutable<PayloadMessage> message, TaskScheduler orleansScheduler)
        {
            if(inputInfo[sourceOperator]==0 && otherTable!=null)
            {
                var batch=otherTable;
                Action action=async ()=>
                {
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

        protected override void BeforeProcessBatch(Immutable<PayloadMessage> message, TaskScheduler orleansScheduler)
        {
            if(sourceOperator==null)
                sourceOperator=message.Value.SenderIdentifer.Split(' ')[0];
        }
    }

}
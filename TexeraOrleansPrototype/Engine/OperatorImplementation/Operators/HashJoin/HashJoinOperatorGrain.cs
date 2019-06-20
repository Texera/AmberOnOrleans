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


        protected override void BeforeProcessBatch(PayloadMessage message)
        {
            string ext;
            isCurrentInnerTable=innerTableGuid.Equals(message.SenderIdentifer.GetPrimaryKey(out ext));
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
                            output.Add(new TexeraTuple(f.FastConcat(tuple.FieldList)));
                        }
                    }
                }
            }
        }

        protected override void AfterProcessBatch(PayloadMessage message)
        {
            if(inputInfo[innerTableGuid]==0 && otherTable!=null)
            {
                // temporarily hold the end flag if ended
                if(currentEndFlagCount==0)
                    currentEndFlagCount=-1;
                var batch=otherTable;
                Action action=()=>
                {
                    isCurrentInnerTable=false;
                    isInnerTableFinished=true;
                    DateTime start=DateTime.UtcNow;
                    List<TexeraTuple> outputList=new List<TexeraTuple>();
                    if(batch!=null)
                    {
                        ProcessBatch(batch,outputList);
                    }
                    if(isPaused)
                    {
                        //if we don't do so, the outputlist will be lost.
                        MakePayloadMessagesThenSend(outputList);
                        taskDidPaused=true;
                        return;
                    }
                    batch=null;
                    //release the end flag if holded
                    if(currentEndFlagCount==-1)
                        currentEndFlagCount=0;
                    currentIndex=0;
                    processTime+=DateTime.UtcNow-start;
                    start=DateTime.UtcNow;
                    MakePayloadMessagesThenSend(outputList);
                    sendingTime+=DateTime.UtcNow-start;
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
                    // if(actionQueue.Count==1)
                    // {
                    //     Task.Run(action);
                    // }
                }
                otherTable=null;
            }
        }
    }
}
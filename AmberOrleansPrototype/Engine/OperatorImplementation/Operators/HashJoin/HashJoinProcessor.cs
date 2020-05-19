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
    public class HashJoinProcessor : ITupleProcessor
    {
        Dictionary<string,List<string[]>> hashTable;
        List<TexeraTuple> otherTable;
        int innerTableIndex=-1;
        int outerTableIndex=-1;
        bool isCurrentInnerTable=false;
        bool isInnerTableFinished=false;
        Guid innerTableGuid=Guid.Empty;
        bool flag = false;
        int currentIndex = 0;
        List<string[]> currentEntry;
        string[] currentTuple;
        Queue<TexeraTuple> resultQueue;
        bool end = false;

        public HashJoinProcessor(int InnerTableIndex, int OuterTableIndex, Guid InnerTableID)
        {
            this.innerTableIndex = InnerTableIndex;
            this.outerTableIndex = OuterTableIndex;
            this.innerTableGuid = InnerTableID;
            Console.WriteLine("HashJoin: innerIdx = "+innerTableIndex+" outerIdx = "+outerTableIndex+" innerGuid = "+InnerTableID);
        }

        public void Accept(TexeraTuple tuple)
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
                        flag = true;
                        currentIndex = 0;
                        currentEntry = hashTable[field];
                        currentTuple = tuple.FieldList;
                    }
                }
            }
        }

        public void OnRegisterSource(Guid from)
        {
            isCurrentInnerTable=innerTableGuid.Equals(from);
        }

        public void NoMore()
        {
           
        }

        public Task Initialize()
        {
            hashTable=new Dictionary<string, List<string[]>>();
            otherTable=new List<TexeraTuple>();
            return Task.CompletedTask;

        }

        public bool HasNext()
        {
            return flag;
           
        }
 
        public TexeraTuple Next()
        {
            var result = new TexeraTuple(currentTuple.FastConcat(currentEntry[currentIndex]));
            currentIndex++;
            if(currentIndex>=currentEntry.Count)
            {
                flag = false;
            }
            return result;
        }

        public void Dispose()
        {
            hashTable=null;
            otherTable=null;
        }

        public Task<TexeraTuple> NextAsync()
        {
            throw new NotImplementedException();
        }

        public void MarkSourceCompleted(Guid source)
        {
            if(innerTableGuid.Equals(source))
            {
                isInnerTableFinished = true;
            }
        }
    }
}
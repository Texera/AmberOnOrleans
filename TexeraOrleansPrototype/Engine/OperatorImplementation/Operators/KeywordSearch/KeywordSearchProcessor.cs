// #define PRINT_MESSAGE_ON
//#define PRINT_DROPPED_ON


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
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{
    public class KeywordSearchProcessor : ITupleProcessor
    {
        int searchIndex;
        string keyword;
        bool flag = false;
        TexeraTuple resultTuple;

        public KeywordSearchProcessor(int searchIndex, string keyword)
        {
            this.searchIndex=searchIndex;
            this.keyword=keyword;
        }

        public void Accept(TexeraTuple tuple)
        {
            if(tuple.FieldList[searchIndex].Contains(keyword))
            {
                flag = true;
                resultTuple = tuple;
            }
        }

        public void Dispose()
        {
            
        }

        public bool HasNext()
        {
            return flag;
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
            
        }

        public void MarkSourceCompleted(Guid source)
        {
            
        }

        public TexeraTuple Next()
        {
            flag=false;
            return resultTuple;
        }

        public Task<TexeraTuple> NextAsync()
        {
            throw new NotImplementedException();
        }

        public void NoMore()
        {
            
        }

        public void OnRegisterSource(Guid from)
        {
            
        }
    }
}
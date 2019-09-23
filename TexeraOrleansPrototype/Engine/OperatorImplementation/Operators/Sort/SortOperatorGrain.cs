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
using System.Reflection;
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{
    public class SortProcessor<T> : ITupleProcessor
    {
        private static MethodInfo ParseInfo = typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
        List<TexeraTuple> sortedTuples;
        List<T> sortedValues;
        int sortIndex;
        bool flag = false;
        int outputIndex = 0;


        public SortProcessor(int sortIndex)
        {
            this.sortIndex=sortIndex;
        }

        private static T Parse(string value)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value;
            else
                return (T)ParseInfo.Invoke(null, new[] { value });
        }

        public void Accept(TexeraTuple tuple)
        {
            T value=Parse(tuple.FieldList[sortIndex]);
            int index = sortedValues.BinarySearch(value);
            if(index<0)
            {
                index=~index;
            }
            sortedTuples.Insert(index,tuple);
            sortedValues.Insert(index,value);
        }

        public void NoMore()
        {
            flag = true;
        }

        public Task Initialize()
        {
            sortedValues=new List<T>();
            sortedTuples=new List<TexeraTuple>();
            return Task.CompletedTask;
            
        }

        public bool HasNext()
        {
            return flag;
        }

        public TexeraTuple Next()
        {
            int i = outputIndex++;
            if(outputIndex>=sortedTuples.Count)
            {
                flag = false;
            }
            return sortedTuples[i];
        }

        public void Dispose()
        {
            sortedValues=null;
            sortedTuples=null;
        }

        public Task<TexeraTuple> NextAsync()
        {
            throw new NotImplementedException();
        }

        public void OnRegisterSource(Guid from)
        {
            
        }

        public void MarkSourceCompleted(Guid source)
        {
            
        }
    }

}
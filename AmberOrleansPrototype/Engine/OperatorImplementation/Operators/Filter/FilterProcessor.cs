using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Orleans.Concurrency;
using Engine.OperatorImplementation.MessagingSemantics;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{

    public class FilterProcessor<T> :ITupleProcessor where T:IComparable<T>
    {
        private static MethodInfo ParseInfo = typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
        int filterIndex=-1;
        FilterType filterType;
        T threshold;
        string thresholdString;
        TexeraTuple resultTuple;
        bool flag = false;
        public FilterProcessor(int filterIndex, FilterType filterType, string threshold)
        {
            if (typeof(T) != typeof(string) && (ParseInfo == null || !typeof(T).IsAssignableFrom(ParseInfo.ReturnType)))
                throw new InvalidOperationException("Invalid type, must contain public static T Parse(string)");
            this.filterType=filterType;
            this.filterIndex=filterIndex;
            this.thresholdString=threshold;
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
            if(tuple.FieldList!=null)
            {
                try
                {
                    switch(filterType)
                    {
                        case FilterType.Equal:
                            if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)==0)
                                flag=true;
                            break;
                        case FilterType.Greater:
                            if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)>0)
                                flag=true;
                            break;
                        case FilterType.GreaterOrEqual:
                            if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)>=0)
                                flag=true;
                            break;
                        case FilterType.Less:
                            if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)<0)
                                flag=true;
                            break;
                        case FilterType.LessOrEqual:
                            if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)<=0)
                                flag=true;
                            break;
                        case FilterType.NotEqual:
                            if(Parse(tuple.FieldList[filterIndex]).CompareTo(threshold)!=0)
                                flag=true;
                            break;
                    }   
                }
                catch (Exception)
                {
                    
                }
                if(flag)
                {
                    resultTuple = tuple;
                }
            }
        }

        public void OnRegisterSource(Guid from)
        {
            
        }

        public void NoMore()
        {
            
        }

        public Task Initialize()
        {
            this.threshold = Parse(thresholdString);
            return Task.CompletedTask;
        }

        public bool HasNext()
        {
            return flag;
        }

        public TexeraTuple Next()
        {
            flag = false;
            return resultTuple;
        }

        public void Dispose()
        {
            
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
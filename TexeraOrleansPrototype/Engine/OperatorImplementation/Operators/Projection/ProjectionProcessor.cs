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
    public class ProjectionProcessor : ITupleProcessor
    {
        List<int> projectionIndexs;
        bool flag = false;
        TexeraTuple resultTuple;
        public ProjectionProcessor(List<int> projectionIndexs)
        {
            this.projectionIndexs=projectionIndexs;
        }
        public void Accept(TexeraTuple tuple)
        {
            try
            {
                resultTuple = new TexeraTuple(new string[projectionIndexs.Count]);
                int i=0;
                foreach(int attr in projectionIndexs)
                {
                    resultTuple.FieldList[i++]=tuple.FieldList[attr];
                }
                flag = true;
            }
            catch(Exception e)
            {
                Console.WriteLine("ERROR in projection: "+String.Join(",",tuple.FieldList));
            }
        }

        public void Dispose()
        {
            projectionIndexs=null;
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
            flag = false;
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
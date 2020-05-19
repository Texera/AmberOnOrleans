using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;
using Orleans.Placement;

namespace Engine.OperatorImplementation.Operators
{
    public class CountFinalProcessor : ITupleProcessor
    {
        public int count = 0;
        private bool flag = false;

        public void Accept(TexeraTuple tuple)
        {
            count+=int.Parse(tuple.FieldList[0]);
        }

        public void OnRegisterSource(Guid from)
        {
            
        }

        public void NoMore()
        {
            flag = true;
        }

        public Task Initialize()
        {
           return Task.CompletedTask; 
        }

        public bool HasNext()
        {
            return flag;
        }

        public TexeraTuple Next()
        {
            flag = false;
            return new TexeraTuple(new string[]{count.ToString()});
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
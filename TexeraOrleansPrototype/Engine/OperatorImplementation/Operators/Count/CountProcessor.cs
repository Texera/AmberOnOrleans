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
using Orleans.Placement;

namespace Engine.OperatorImplementation.Operators
{
    public class CountProcessor : ITupleProcessor
    {
        int count=0;
        bool flag=false;

        public void Accept(TexeraTuple tuple)
        {
            count++;
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
            return new TexeraTuple(new string[]{count.ToString()});
        }

        public Task<TexeraTuple> NextAsync()
        {
            throw new NotImplementedException();
        }

        public void NoMore()
        {
            flag=true;
        }

        public void OnRegisterSource(Guid from)
        {
            
        }
    }
}